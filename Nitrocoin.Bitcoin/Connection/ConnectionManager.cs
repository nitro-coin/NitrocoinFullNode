﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using NBitcoin;
using NBitcoin.Protocol;
using NBitcoin.Protocol.Behaviors;
using Nitrocoin.Bitcoin.BlockPulling;
using Nitrocoin.Bitcoin.Configuration;
using Nitrocoin.Bitcoin.Connection;
using Nitrocoin.Bitcoin.Logging;
using Nitrocoin.Bitcoin.Configuration.Settings;

namespace Nitrocoin.Bitcoin.Connection
{
	public class ConnectionManager : IDisposable
	{
		// The maximum number of entries in an 'inv' protocol message 
		public const int MAX_INV_SZ = 50000;

		private readonly NodesCollection connectedNodes = new NodesCollection();
		private readonly Dictionary<Node, PerformanceSnapshot> downloads = new Dictionary<Node, PerformanceSnapshot>();
		private NodeServices discoveredNodeRequiredService = NodeServices.Network;
		private readonly ConnectionManagerSettings connectionManagerSettings;
		private readonly Network network;
		private readonly NodeConnectionParameters parameters;
		private readonly NodeSettings nodeSettings;

		public Network Network { get { return this.network; } }
		public NodeConnectionParameters Parameters { get { return this.parameters; } }
		public NodeSettings NodeSettings { get { return this.nodeSettings; } }
		public IReadOnlyNodesCollection ConnectedNodes { get { return this.connectedNodes; } }
		public List<NodeServer> Servers { get; } = new List<NodeServer>();
		public NodesGroup ConnectNodeGroup { get; private set; }
		public NodesGroup AddNodeNodeGroup { get; private set; }
		public NodesGroup DiscoveredNodeGroup { get; set; }

		public ConnectionManager(Network network, NodeConnectionParameters parameters, NodeSettings nodeSettings)
		{
			this.network = network;
			this.nodeSettings = nodeSettings;
			this.connectionManagerSettings = nodeSettings.ConnectionManager;
			this.parameters = parameters;
		}

		public void Start()
		{
			this.parameters.UserAgent = "NitrocoinBitcoin:" + GetVersion();
			this.parameters.Version = this.NodeSettings.ProtocolVersion;
			if (this.connectionManagerSettings.Connect.Count == 0)
			{
				NodeConnectionParameters cloneParameters = this.parameters.Clone();
				cloneParameters.TemplateBehaviors.Add(new ConnectionManagerBehavior(false, this));
				this.DiscoveredNodeGroup = CreateNodeGroup(cloneParameters, this.discoveredNodeRequiredService);
				this.DiscoveredNodeGroup.CustomGroupSelector = WellKnownGroupSelectors.ByNetwork; //is the default, but I want to use it
				this.DiscoveredNodeGroup.Connect();
			}
			else
			{
				NodeConnectionParameters cloneParameters = this.parameters.Clone();
				cloneParameters.TemplateBehaviors.Add(new ConnectionManagerBehavior(false, this));
				cloneParameters.TemplateBehaviors.Remove<AddressManagerBehavior>();
				var addrman = new AddressManager();
				addrman.Add(this.connectionManagerSettings.Connect.Select(c => new NetworkAddress(c)).ToArray(), IPAddress.Loopback);
				var addrmanBehavior = new AddressManagerBehavior(addrman);
				addrmanBehavior.Mode = AddressManagerBehaviorMode.None;
				cloneParameters.TemplateBehaviors.Add(addrmanBehavior);

				this.ConnectNodeGroup = CreateNodeGroup(cloneParameters, NodeServices.Nothing);
				this.ConnectNodeGroup.MaximumNodeConnection = this.connectionManagerSettings.Connect.Count;
				this.ConnectNodeGroup.CustomGroupSelector = WellKnownGroupSelectors.ByEndpoint;
				this.ConnectNodeGroup.Connect();
			}

			{
				NodeConnectionParameters cloneParameters = this.parameters.Clone();
				cloneParameters.TemplateBehaviors.Add(new ConnectionManagerBehavior(false, this));
				cloneParameters.TemplateBehaviors.Remove<AddressManagerBehavior>();
				var addrman = new AddressManager();
				addrman.Add(this.connectionManagerSettings.AddNode.Select(c => new NetworkAddress(c)).ToArray(), IPAddress.Loopback);
				var addrmanBehavior = new AddressManagerBehavior(addrman);
				addrmanBehavior.Mode = AddressManagerBehaviorMode.AdvertizeDiscover;
				cloneParameters.TemplateBehaviors.Add(addrmanBehavior);

				this.AddNodeNodeGroup = CreateNodeGroup(cloneParameters, NodeServices.Nothing);
				this.AddNodeNodeGroup.MaximumNodeConnection = this.connectionManagerSettings.AddNode.Count;
				this.AddNodeNodeGroup.CustomGroupSelector = WellKnownGroupSelectors.ByEndpoint;
				this.AddNodeNodeGroup.Connect();
			}

			StringBuilder logs = new StringBuilder();
			logs.AppendLine("Node listening on:");
			foreach (NodeServerEndpoint listen in this.connectionManagerSettings.Listen)
			{
				NodeConnectionParameters cloneParameters = this.parameters.Clone();
				var server = new NodeServer(this.Network);
				server.LocalEndpoint = listen.Endpoint;
				server.ExternalEndpoint = this.connectionManagerSettings.ExternalEndpoint;
				this.Servers.Add(server);
				cloneParameters.TemplateBehaviors.Add(new ConnectionManagerBehavior(true, this)
				{
					Whitelisted = listen.Whitelisted
				});
				server.InboundNodeConnectionParameters = cloneParameters;
				server.Listen();
				logs.Append(listen.Endpoint.Address + ":" + listen.Endpoint.Port);
				if (listen.Whitelisted)
					logs.Append(" (whitelisted)");
				logs.AppendLine();
			}
			Logs.ConnectionManager.LogInformation(logs.ToString());
		}

		public void AddDiscoveredNodesRequirement(NodeServices services)
		{
			this.discoveredNodeRequiredService |= services;
			NodesGroup group = this.DiscoveredNodeGroup;
			if (group != null &&
			   !group.Requirements.RequiredServices.HasFlag(services))
			{
				group.Requirements.RequiredServices |= NodeServices.NODE_WITNESS;
				foreach (Node node in group.ConnectedNodes)
				{
					if (!node.PeerVersion.Services.HasFlag(services))
						node.DisconnectAsync("The peer does not support the required services requirement");
				}
			}
		}

		public string GetStats()
		{
			StringBuilder builder = new StringBuilder();
			lock (this.downloads)
			{
				PerformanceSnapshot diffTotal = new PerformanceSnapshot(0, 0);
				builder.AppendLine("=======Connections=======");
				foreach (Node node in this.ConnectedNodes)
				{
					PerformanceSnapshot newSnapshot = node.Counter.Snapshot();
					PerformanceSnapshot lastSnapshot = null;
					if (this.downloads.TryGetValue(node, out lastSnapshot))
					{
						BlockPuller.BlockPullerBehavior behavior = node.Behaviors.OfType<BlockPuller.BlockPullerBehavior>()
																	.FirstOrDefault(b => b.Puller.GetType() == typeof(LookaheadBlockPuller));
						PerformanceSnapshot diff = newSnapshot - lastSnapshot;
						diffTotal = new PerformanceSnapshot(diff.TotalReadenBytes + diffTotal.TotalReadenBytes, diff.TotalWrittenBytes + diffTotal.TotalWrittenBytes) { Start = diff.Start, Taken = diff.Taken };
						builder.Append((node.RemoteSocketAddress + ":" + node.RemoteSocketPort).PadRight(Logs.ColumnLength * 2) + "R:" + ToKBSec(diff.ReadenBytesPerSecond) + "\tW:" + ToKBSec(diff.WrittenBytesPerSecond));
						if (behavior != null)
						{
							builder.Append("\tQualityScore: " + behavior.QualityScore + (behavior.QualityScore < 10 ? "\t" : "") + "\tPendingBlocks: " + behavior.PendingDownloads.Count);
						}
						builder.AppendLine();
					}
					this.downloads.AddOrReplace(node, newSnapshot);
				}
				builder.AppendLine("=================");
				builder.AppendLine("Total".PadRight(Logs.ColumnLength * 2) + "R:" + ToKBSec(diffTotal.ReadenBytesPerSecond) + "\tW:" + ToKBSec(diffTotal.WrittenBytesPerSecond));
				builder.AppendLine("==========================");

				//TODO: Hack, we should just clean nodes that are not connect anymore
				if (this.downloads.Count > 1000)
					this.downloads.Clear();
			}
			return builder.ToString();
		}

		public string GetNodeStats()
		{
			var builder = new StringBuilder();

			foreach (Node node in this.ConnectedNodes)
			{
				ConnectionManagerBehavior connectionManagerBehavior = node.Behavior<ConnectionManagerBehavior>();
				BlockStore.ChainBehavior chainBehavior = node.Behavior<BlockStore.ChainBehavior>();
				builder.AppendLine(
					"Node:" + (node.RemoteInfo() + ", ").PadRight(Logs.ColumnLength + 15) +
					(" connected" + " (" + (connectionManagerBehavior.Inbound ? "inbound" : "outbound") + "),").PadRight(Logs.ColumnLength + 7) +
					(" agent " + node.PeerVersion.UserAgent + ", ").PadRight(Logs.ColumnLength + 2) +
					" height=" + chainBehavior.PendingTip.Height);
			}
			return builder.ToString();
		}

		private string ToKBSec(ulong bytesPerSec)
		{
			double speed = ((double)bytesPerSec / 1024.0);
			return speed.ToString("0.00") + " KB/S";
		}

		private NodesGroup CreateNodeGroup(NodeConnectionParameters cloneParameters, NodeServices requiredServices)
		{
			return new NodesGroup(this.Network, cloneParameters, new NodeRequirement()
			{
				MinVersion = this.NodeSettings.ProtocolVersion,
				RequiredServices = requiredServices,
			});
		}

		private string GetVersion()
		{
			Match match = Regex.Match(this.GetType().AssemblyQualifiedName, "Version=([0-9]+\\.[0-9]+\\.[0-9]+)\\.");
			return match.Groups[1].Value;
		}

		public void Dispose()
		{
			if (this.DiscoveredNodeGroup != null)
				this.DiscoveredNodeGroup.Dispose();
			if (this.ConnectNodeGroup != null)
				this.ConnectNodeGroup.Dispose();
			if (this.AddNodeNodeGroup != null)
				this.AddNodeNodeGroup.Dispose();
			foreach (NodeServer server in this.Servers)
				server.Dispose();
			foreach (Node node in this.connectedNodes.Where(n => n.Behaviors.Find<ConnectionManagerBehavior>().OneTry))
				node.Disconnect();
		}

		internal void AddConnectedNode(Node node)
		{
			this.connectedNodes.Add(node);
		}

		internal void RemoveConnectedNode(Node node)
		{
			this.connectedNodes.Remove(node);
		}

		public Node FindNodeByEndpoint(IPEndPoint endpoint)
		{
			return this.connectedNodes.FindByEndpoint(endpoint);
		}

		public Node FindNodeByIp(IPAddress ip)
		{
			return this.connectedNodes.FindByIp(ip);
		}

		public Node FindLocalNode()
		{
			return this.connectedNodes.FindLocal();
		}

		public void AddNodeAddress(IPEndPoint endpoint)
		{
			AddressManager addrman = AddressManagerBehavior.GetAddrman(this.AddNodeNodeGroup.NodeConnectionParameters);
			addrman.Add(new NetworkAddress(endpoint));
			this.AddNodeNodeGroup.MaximumNodeConnection++;
		}

		public void RemoveNodeAddress(IPEndPoint endpoint)
		{
			Node node = this.connectedNodes.FindByEndpoint(endpoint);
			if (node != null)
				node.DisconnectAsync("Requested by user");
		}

		public Node Connect(IPEndPoint endpoint)
		{
			NodeConnectionParameters cloneParameters = this.parameters.Clone();
			cloneParameters.TemplateBehaviors.Add(new ConnectionManagerBehavior(false, this)
			{
				OneTry = true
			});
			var node = Node.Connect(this.Network, endpoint, cloneParameters);
			node.VersionHandshake();
			return node;
		}
	}
}
