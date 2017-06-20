using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NBitcoin;
using NBitcoin.Protocol;
using Nitrocoin.Bitcoin.BlockPulling;
using Nitrocoin.Bitcoin.BlockStore;
using Nitrocoin.Bitcoin.Builder;
using Nitrocoin.Bitcoin.Builder.Feature;
using Nitrocoin.Bitcoin.Configuration;
using Nitrocoin.Bitcoin.Connection;
using Nitrocoin.Bitcoin.Logging;
using System;
using System.Linq;
using System.Threading;
using static Nitrocoin.Bitcoin.FullNode;

namespace Nitrocoin.Bitcoin.Miner
{
	public class MiningFeature : FullNodeFeature
	{		
		public override void Start()
		{
		}

		public override void Stop()
		{
		}
	}

	public static class MiningFeatureExtension
	{
		public static IFullNodeBuilder AddMining(this IFullNodeBuilder fullNodeBuilder)
		{
			fullNodeBuilder.ConfigureFeature(features =>
			{
				features
					.AddFeature<MiningFeature>()
					.FeatureServices(services =>
					{
						services.AddSingleton<PowMining>();
						services.AddSingleton<AssemblerFactory, PowAssemblerFactory>();
					});
			});

			return fullNodeBuilder;
		}

		public static IFullNodeBuilder AddPowPosMining(this IFullNodeBuilder fullNodeBuilder)
		{
			fullNodeBuilder.ConfigureFeature(features =>
			{
				features
					.AddFeature<MiningFeature>()
					.FeatureServices(services =>
					{
						services.AddSingleton<PowMining>();
						services.AddSingleton<PosMinting>();
						services.AddSingleton<AssemblerFactory, PosAssemblerFactory>();
					});
			});

			return fullNodeBuilder;
		}
	}
}
