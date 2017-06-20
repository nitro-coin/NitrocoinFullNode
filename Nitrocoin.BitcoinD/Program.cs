using Microsoft.Extensions.Logging;
using Nitrocoin.Bitcoin.Builder;
using Nitrocoin.Bitcoin.Configuration;
using Nitrocoin.Bitcoin.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Nitrocoin.Bitcoin.BlockStore;
using Nitrocoin.Bitcoin.MemoryPool;
using Nitrocoin.Bitcoin.Consensus;
using Nitrocoin.Bitcoin.RPC;
using Nitrocoin.Bitcoin.Miner;
using NBitcoin;
using Nitrocoin.Bitcoin.Utilities;

namespace Nitrocoin.BitcoinD
{
	public class Program
	{
		public static void Main(string[] args)
		{
			Logs.Configure(Logs.GetLoggerFactory(args));

			if (NodeSettings.PrintHelp(args, Network.Main))
				return;
			
			NodeSettings nodeSettings = NodeSettings.FromArguments(args);

			if (!Checks.VerifyAccess(nodeSettings))
				return;

			var node = new FullNodeBuilder()
				.UseNodeSettings(nodeSettings)
				.UseConsensus()
				.UseBlockStore()
				.UseMempool()
				.AddMining()
				.AddRPC()
				.Build();

			// start the miner (this is temporary a miner should be started using RPC.
			Task.Delay(TimeSpan.FromMinutes(1)).ContinueWith((t) => { TryStartMiner(args, node); });

			node.Run();
		}

		private static void TryStartMiner(string[] args, IFullNode node)
		{
			// mining can be called from either RPC or on start
			// to manage the on strat we need to get an address to the mining code
			var mine = args.FirstOrDefault(a => a.Contains("mine="));
			if (mine != null)
			{
				// get the address to mine to
				var addres = mine.Replace("mine=", string.Empty);
				var pubkey = BitcoinAddress.Create(addres, node.Network);
				node.Services.ServiceProvider.Service<PowMining>().Mine(pubkey.ScriptPubKey);
			}
		}
	}
}
