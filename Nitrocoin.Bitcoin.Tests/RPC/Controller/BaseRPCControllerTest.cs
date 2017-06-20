using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Nitrocoin.Bitcoin.BlockStore;
using Nitrocoin.Bitcoin.Builder;
using Nitrocoin.Bitcoin.Configuration;
using Nitrocoin.Bitcoin.Connection;
using Nitrocoin.Bitcoin.Consensus;
using Nitrocoin.Bitcoin.Logging;
using Nitrocoin.Bitcoin.MemoryPool;
using Nitrocoin.Bitcoin.RPC;
using Nitrocoin.Bitcoin.RPC.Controllers;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Nitrocoin.Bitcoin.Tests.RPC.Controller
{
    public abstract class BaseRPCControllerTest : TestBase
    {
        public BaseRPCControllerTest()
        {
            Logs.Configure(new LoggerFactory());
        }

        public IFullNode BuildServicedNode(string dir)
        {
            var nodeSettings = NodeSettings.Default();
            nodeSettings.DataDir = dir;
            var fullNodeBuilder = new FullNodeBuilder(nodeSettings);
            IFullNode fullNode = fullNodeBuilder
                .UseConsensus()
                .UseBlockStore()
                .UseMempool()
                .AddRPC()
                .Build();

            return fullNode;
        }
    }
}
