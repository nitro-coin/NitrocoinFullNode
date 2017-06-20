using Microsoft.Extensions.DependencyInjection;
using NBitcoin;
using Nitrocoin.Bitcoin.Builder;
using Nitrocoin.Bitcoin.RPC.Controllers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nitrocoin.Bitcoin.Logging;
using Xunit;

namespace Nitrocoin.Bitcoin.Tests.RPC.Controller
{
    public class MempoolActionTests : BaseRPCControllerTest
    {
        [Fact]
        public async Task CanCall()
        {
			Logs.Configure(new LoggerFactory());

			string dir = AssureEmptyDir("Nitrocoin.Bitcoin.Tests/TestData/GetRawMempoolActionTest/CanCall");
            IFullNode fullNode = this.BuildServicedNode(dir);
            MempoolController controller = fullNode.Services.ServiceProvider.GetService<MempoolController>();

            List<uint256> result = await controller.GetRawMempool();

            Assert.NotNull(result);
        }
    }
}
