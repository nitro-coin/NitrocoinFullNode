using Microsoft.Extensions.DependencyInjection;
using NBitcoin;
using Nitrocoin.Bitcoin.Builder;
using Nitrocoin.Bitcoin.RPC.Controllers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Nitrocoin.Bitcoin.Tests.RPC.Controller
{
    public class ConsensusActionTests : BaseRPCControllerTest
    {
        private IFullNode fullNode;
        private ConsensusController controller;

        public ConsensusActionTests()
        {
            string dir = "Nitrocoin.Bitcoin.Tests/TestData/ConsensusActionTests";
            this.fullNode = this.BuildServicedNode(dir);
            this.controller = this.fullNode.Services.ServiceProvider.GetService<ConsensusController>();
        }

        [Fact]
        public void CanCall_GetBestBlockHash()
        {
            uint256 result = this.controller.GetBestBlockHash();

            Assert.Null(result);
        }

        [Fact]
        public void CanCall_GetBlockHash()
        {
            uint256 result = this.controller.GetBlockHash(0);

            Assert.Null(result);
        }
    }
}
