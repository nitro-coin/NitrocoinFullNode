using System.Collections.Generic;
using System.Net;

namespace Nitrocoin.Bitcoin.Configuration.Settings
{
	public class ConnectionManagerSettings
	{
		public ConnectionManagerSettings()
		{
		}
		public List<IPEndPoint> Connect
		{
			get; set;
		} = new List<IPEndPoint>();
		public List<IPEndPoint> AddNode
		{
			get; set;
		} = new List<IPEndPoint>();
		public List<NodeServerEndpoint> Listen
		{
			get; set;
		} = new List<NodeServerEndpoint>();
		public IPEndPoint ExternalEndpoint
		{
			get;
			internal set;
		}
	}
}