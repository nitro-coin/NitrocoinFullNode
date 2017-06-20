using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nitrocoin.Bitcoin.Logging
{
    public class Logs
    {
        public static void Configure(ILoggerFactory factory)
        {
            LoggerFactory = factory;

            // These match namespace; classes can also use CreateLogger<T>, which will inherit
            Configuration = factory.CreateLogger("Nitrocoin.Bitcoin.Configuration");
            RPC = factory.CreateLogger("Nitrocoin.Bitcoin.RPC");
            FullNode = factory.CreateLogger("Nitrocoin.Bitcoin.FullNode");
            ConnectionManager = factory.CreateLogger("Nitrocoin.Bitcoin.Connection");
            Bench = factory.CreateLogger("Nitrocoin.Bitcoin.FullNode.ConsensusStats");
            Mempool = factory.CreateLogger("Nitrocoin.Bitcoin.MemoryPool");
            BlockStore = factory.CreateLogger("Nitrocoin.Bitcoin.BlockStore");
            Consensus = factory.CreateLogger("Nitrocoin.Bitcoin.Consensus");
            EstimateFee = factory.CreateLogger("Nitrocoin.Bitcoin.Fee");
            Mining = factory.CreateLogger("Nitrocoin.Bitcoin.Mining");
            Notifications = factory.CreateLogger("Nitrocoin.Bitcoin.Notifications");
        }

        public static ILoggerFactory GetLoggerFactory(string[] args)
        {
            // TODO: preload enough args for -conf= or -datadir= to get debug args from there
            // TODO: currently only takes -debug arg
            var debugArgs = args.Where(a => a.StartsWith("-debug=")).Select(a => a.Substring("-debug=".Length).Replace("\"", "")).FirstOrDefault();

            var keyToCategory = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                //{ "addrman", "" },
                //{ "alert", "" },
                { "bench", "Nitrocoin.Bitcoin.FullNode.ConsensusStats" },
                //{ "coindb", "" },
                { "db", "Nitrocoin.Bitcoin.BlockStore" }, 
                //{ "lock", "" }, 
                //{ "rand", "" }, 
                { "rpc", "Nitrocoin.Bitcoin.RPC" }, 
                //{ "selectcoins", "" }, 
                { "mempool", "Nitrocoin.Bitcoin.MemoryPool" }, 
                //{ "mempoolrej", "" }, 
                { "net", "Nitrocoin.Bitcoin.Connection" }, 
                //{ "proxy", "" }, 
                //{ "prune", "" }, 
                //{ "http", "" }, 
                //{ "libevent", "" }, 
                //{ "tor", "" }, 
                //{ "zmq", "" }, 
                //{ "qt", "" },

                // Short Names
                { "estimatefee", "Nitrocoin.Bitcoin.Fee" },
                { "configuration", "Nitrocoin.Bitcoin.Configuration" },
                { "fullnode", "Nitrocoin.Bitcoin.FullNode" },
                { "consensus", "Nitrocoin.Bitcoin.FullNode" },
                { "mining", "Nitrocoin.Bitcoin.FullNode" },
	            { "wallet", "Nitrocoin.Bitcoin.Wallet" },
			};
            var filterSettings = new FilterLoggerSettings();
            // Default level is Information
            filterSettings.Add("Default", LogLevel.Information);
            // TODO: Probably should have a way to configure these as well
            filterSettings.Add("System", LogLevel.Warning);
            filterSettings.Add("Microsoft", LogLevel.Warning);
            // Disable aspnet core logs (retained from ASP.NET config)
            filterSettings.Add("Microsoft.AspNetCore", LogLevel.Error);

            if (!string.IsNullOrWhiteSpace(debugArgs))
            {
                if (debugArgs.Trim() == "1")
                {
                    // Increase all logging to Trace
                    filterSettings.Add("Nitrocoin.Bitcoin", LogLevel.Trace);
                }
                else
                {
                    // Increase selected categories to Trace
                    var categoryKeys = debugArgs.Split(',');
                    foreach (var key in categoryKeys)
                    {
                        string category;
                        if (keyToCategory.TryGetValue(key.Trim(), out category))
                        {
                            filterSettings.Add(category, LogLevel.Trace);
                        }
                        else
                        {
                            // Can directly specify something like -debug=Nitrocoin.Bitcoin.Miner
                            filterSettings.Add(key, LogLevel.Trace);
                        }
                    }
                }
            }

            // TODO: Additional args
            //var logipsArgs = args.Where(a => a.StartsWith("-logips=")).Select(a => a.Substring("-logips=".Length).Replace("\"", "")).FirstOrDefault();
            //var printtoconsoleArgs = args.Where(a => a.StartsWith("-printtoconsole=")).Select(a => a.Substring("-printtoconsole=".Length).Replace("\"", "")).FirstOrDefault();

            ILoggerFactory loggerFactory = new LoggerFactory()
                .WithFilter(filterSettings);
            loggerFactory.AddDebug(LogLevel.Trace);
            loggerFactory.AddConsole(LogLevel.Trace);
	        loggerFactory.AddFile("Logs/node-{Date}.json", isJson: true, minimumLevel: LogLevel.Debug,
		        fileSizeLimitBytes: 10000000);

			return loggerFactory;
        }

        public static ILogger Configuration
        {
            get; private set;
        }

        public static ILogger RPC
        {
            get; private set;
        }

        public static ILogger FullNode
        {
            get; private set;
        }

        public static ILogger ConnectionManager
        {
            get; private set;
        }

        public static ILogger Bench
        {
            get; private set;
        }

        public static ILogger Mempool
        {
            get; set;
        }
        public static ILogger BlockStore
        {
            get; private set;
        }

        public static ILogger EstimateFee
        {
            get; private set;
        }

        public static ILoggerFactory LoggerFactory
        {
            get; private set;
        }

        public static ILogger Consensus
        {
            get; set;
        }

        public static ILogger Mining
        {
            get; set;
        }

        public static ILogger Notifications
        {
            get; set;
        }


        public const int ColumnLength = 16;
    }
}
