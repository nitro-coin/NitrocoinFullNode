using Microsoft.Extensions.DependencyInjection;
using Nitrocoin.Bitcoin.Builder;
using Nitrocoin.Bitcoin.RPC.Controllers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Nitrocoin.Bitcoin.Tests.RPC.Controller
{
    public class AddNodeActionTest : BaseRPCControllerTest
    {
        [Fact]
        public void CanCall()
        {
            string dir = AssureEmptyDir("Nitrocoin.Bitcoin.Tests/TestData/AddNodeActionTest/CanCall");
            IFullNode fullNode = this.BuildServicedNode(dir);
            ConnectionManagerController controller = fullNode.Services.ServiceProvider.GetService<ConnectionManagerController>();

            Assert.Throws(typeof(System.Net.Sockets.SocketException), () => { controller.AddNode("0.0.0.0", "onetry"); });
            Assert.Throws(typeof(ArgumentException), () => { controller.AddNode("0.0.0.0", "notarealcommand"); });
            Assert.Throws(typeof(FormatException), () => { controller.AddNode("a.b.c.d", "onetry"); });
            Assert.True(controller.AddNode("0.0.0.0", "remove"));
        }

    }
}
