﻿using System.Buffers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;
using Nitrocoin.Bitcoin.RPC;
using Xunit;

namespace Nitrocoin.Bitcoin.Tests.RPC
{
    public class RPCJsonMvcOptionsSetupTest
    {
        [Fact]
        public void ConfigureMvcReplacesJsonFormattedWithRPCJsonOutputFormatter()
        {
            var settings = new JsonSerializerSettings();
            var charpool = ArrayPool<char>.Create();
            var options = new MvcOptions();
            options.OutputFormatters.Clear();
            options.OutputFormatters.Add(new JsonOutputFormatter(settings, charpool));

            RPCJsonMvcOptionsSetup.ConfigureMvc(options, settings, null, charpool, null);

            Assert.Equal(1, options.OutputFormatters.Count);
            Assert.Equal(typeof(RPCJsonOutputFormatter), options.OutputFormatters[0].GetType());
        }
    }
}
