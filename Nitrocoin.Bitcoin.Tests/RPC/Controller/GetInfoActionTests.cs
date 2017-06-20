using Microsoft.Extensions.DependencyInjection;
using Nitrocoin.Bitcoin.Builder;
using Nitrocoin.Bitcoin.Configuration;
using Nitrocoin.Bitcoin.MemoryPool;
using Nitrocoin.Bitcoin.RPC.Controllers;
using Nitrocoin.Bitcoin.RPC.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Nitrocoin.Bitcoin.Tests.RPC.Controller
{
    public class GetInfoActionTests : BaseRPCControllerTest
    {
        [Fact]
        public void CallWithDependencies()
        {
            string dir = AssureEmptyDir("Nitrocoin.Bitcoin.Tests/TestData/GetInfoActionTests/CallWithDependencies");
            IFullNode fullNode = this.BuildServicedNode(dir);
            FullNodeController controller = fullNode.Services.ServiceProvider.GetService<FullNodeController>();

            GetInfoModel info = controller.GetInfo();

            uint expectedProtocolVersion = (uint)NodeSettings.Default().ProtocolVersion;
            var expectedRelayFee = MempoolValidator.MinRelayTxFee.FeePerK.ToUnit(NBitcoin.MoneyUnit.BTC);
            Assert.NotNull(info);
            Assert.Equal(0, info.blocks);
            Assert.NotEqual<uint>(0, info.version);
            Assert.Equal(expectedProtocolVersion, info.protocolversion);
            Assert.Equal(0, info.timeoffset);
            Assert.Equal(0, info.connections);
            Assert.NotNull(info.proxy);
            Assert.Equal(0, info.difficulty);
            Assert.False(info.testnet);
            Assert.Equal(expectedRelayFee, info.relayfee);
            Assert.Empty(info.errors);
        }

    }
}
