using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nitrocoin.Bitcoin.Connection;
using Xunit;

namespace Nitrocoin.Bitcoin.IntegrationTests
{
    public class NodeSync
    {
        [Fact]
        public void NodesCanConnectToEachOthers()
        {
            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var node1 = builder.CreateNitrocoinNode();
                var node2 = builder.CreateNitrocoinNode();
                builder.StartAll();
                Assert.Equal(0, node1.FullNode.ConnectionManager.ConnectedNodes.Count());
                Assert.Equal(0, node2.FullNode.ConnectionManager.ConnectedNodes.Count());
                var rpc1 = node1.CreateRPCClient();
                var rpc2 = node2.CreateRPCClient();
                rpc1.AddNode(node2.Endpoint, true);
                Assert.Equal(1, node1.FullNode.ConnectionManager.ConnectedNodes.Count());
                Assert.Equal(1, node2.FullNode.ConnectionManager.ConnectedNodes.Count());

                var behavior = node1.FullNode.ConnectionManager.ConnectedNodes.First().Behaviors.Find<ConnectionManagerBehavior>();
                Assert.False(behavior.Inbound);
                Assert.True(behavior.OneTry);
                behavior = node2.FullNode.ConnectionManager.ConnectedNodes.First().Behaviors.Find<ConnectionManagerBehavior>();
                Assert.True(behavior.Inbound);
                Assert.False(behavior.OneTry);
            }
        }

        [Fact]
        public void CanNitrocoinSyncFromCore()
        {
            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var NitrocoinNode = builder.CreateNitrocoinNode();
                var coreNode = builder.CreateNode();
                builder.StartAll();

                // not in IBD
                NitrocoinNode.FullNode.ChainBehaviorState.SetIsInitialBlockDownload(false, DateTime.UtcNow.AddMinutes(5));

                var tip = coreNode.FindBlock(10).Last();
                NitrocoinNode.CreateRPCClient().AddNode(coreNode.Endpoint, true);
                TestHelper.WaitLoop(() => NitrocoinNode.CreateRPCClient().GetBestBlockHash() == coreNode.CreateRPCClient().GetBestBlockHash());
                var bestBlockHash = NitrocoinNode.CreateRPCClient().GetBestBlockHash();
                Assert.Equal(tip.GetHash(), bestBlockHash);

                //Now check if Core connect to Nitrocoin
                NitrocoinNode.CreateRPCClient().RemoveNode(coreNode.Endpoint);
                tip = coreNode.FindBlock(10).Last();
                coreNode.CreateRPCClient().AddNode(NitrocoinNode.Endpoint, true);
                TestHelper.WaitLoop(() => NitrocoinNode.CreateRPCClient().GetBestBlockHash() == coreNode.CreateRPCClient().GetBestBlockHash());
                bestBlockHash = NitrocoinNode.CreateRPCClient().GetBestBlockHash();
                Assert.Equal(tip.GetHash(), bestBlockHash);
            }
        }

        [Fact]
        public void CanNitrocoinSyncFromNitrocoin()
        {
            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var NitrocoinNode = builder.CreateNitrocoinNode();
                var NitrocoinNodeSync = builder.CreateNitrocoinNode();
                var coreCreateNode = builder.CreateNode();
                builder.StartAll();

                // not in IBD
                NitrocoinNode.FullNode.ChainBehaviorState.SetIsInitialBlockDownload(false, DateTime.UtcNow.AddMinutes(5));
                NitrocoinNodeSync.FullNode.ChainBehaviorState.SetIsInitialBlockDownload(false, DateTime.UtcNow.AddMinutes(5));

                // first seed a core node with blocks and sync them to a Nitrocoin node
                // and wait till the Nitrocoin node is fully synced
                var tip = coreCreateNode.FindBlock(5).Last();
                NitrocoinNode.CreateRPCClient().AddNode(coreCreateNode.Endpoint, true);
                TestHelper.WaitLoop(() => NitrocoinNode.CreateRPCClient().GetBestBlockHash() == coreCreateNode.CreateRPCClient().GetBestBlockHash());
                var bestBlockHash = NitrocoinNode.CreateRPCClient().GetBestBlockHash();
                Assert.Equal(tip.GetHash(), bestBlockHash);

                // add a new Nitrocoin node which will download
                // the blocks using the GetData payload
                NitrocoinNodeSync.CreateRPCClient().AddNode(NitrocoinNode.Endpoint, true);

                // wait for download and assert
                TestHelper.WaitLoop(() => NitrocoinNode.CreateRPCClient().GetBestBlockHash() == NitrocoinNodeSync.CreateRPCClient().GetBestBlockHash());
                bestBlockHash = NitrocoinNodeSync.CreateRPCClient().GetBestBlockHash();
                Assert.Equal(tip.GetHash(), bestBlockHash);

            }
        }

        [Fact]
        public void CanCoreSyncFromNitrocoin()
        {
            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var NitrocoinNode = builder.CreateNitrocoinNode();
                var coreNodeSync = builder.CreateNode();
                var coreCreateNode = builder.CreateNode();
                builder.StartAll();

                // not in IBD
                NitrocoinNode.FullNode.ChainBehaviorState.SetIsInitialBlockDownload(false, DateTime.UtcNow.AddMinutes(5));

                // first seed a core node with blocks and sync them to a Nitrocoin node
                // and wait till the Nitrocoin node is fully synced
                var tip = coreCreateNode.FindBlock(5).Last();
                NitrocoinNode.CreateRPCClient().AddNode(coreCreateNode.Endpoint, true);
                TestHelper.WaitLoop(() => NitrocoinNode.CreateRPCClient().GetBestBlockHash() == coreCreateNode.CreateRPCClient().GetBestBlockHash());
                TestHelper.WaitLoop(() => NitrocoinNode.FullNode.ChainBehaviorState.HighestPersistedBlock.HashBlock == NitrocoinNode.FullNode.Chain.Tip.HashBlock);

                var bestBlockHash = NitrocoinNode.CreateRPCClient().GetBestBlockHash();
                Assert.Equal(tip.GetHash(), bestBlockHash);

                // add a new Nitrocoin node which will download
                // the blocks using the GetData payload
                coreNodeSync.CreateRPCClient().AddNode(NitrocoinNode.Endpoint, true);

                // wait for download and assert
                TestHelper.WaitLoop(() => NitrocoinNode.CreateRPCClient().GetBestBlockHash() == coreNodeSync.CreateRPCClient().GetBestBlockHash());
                bestBlockHash = coreNodeSync.CreateRPCClient().GetBestBlockHash();
                Assert.Equal(tip.GetHash(), bestBlockHash);
            }
        }

    }
}
