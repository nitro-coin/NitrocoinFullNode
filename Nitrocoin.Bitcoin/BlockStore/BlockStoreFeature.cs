﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NBitcoin;
using NBitcoin.Protocol;
using Nitrocoin.Bitcoin.BlockPulling;
using Nitrocoin.Bitcoin.Builder;
using Nitrocoin.Bitcoin.Builder.Feature;
using Nitrocoin.Bitcoin.Configuration;
using Nitrocoin.Bitcoin.Connection;
using Nitrocoin.Bitcoin.Logging;

namespace Nitrocoin.Bitcoin.BlockStore
{
	public class BlockStoreFeature : FullNodeFeature 
	{
		private readonly ConcurrentChain chain;
		private readonly Signals signals;
		private readonly BlockRepository blockRepository;
		private readonly BlockStoreCache blockStoreCache;
		private readonly StoreBlockPuller blockPuller;
		private readonly BlockStoreLoop blockStoreLoop;
		private readonly BlockStoreManager blockStoreManager;
		private readonly BlockStoreSignaled blockStoreSignaled;
		private readonly FullNode.CancellationProvider cancellationProvider;
		private readonly ConnectionManager connectionManager;
		private readonly NodeSettings nodeSettings;

		public BlockStoreFeature(ConcurrentChain chain, ConnectionManager connectionManager, Signals signals, BlockRepository blockRepository,  
			BlockStoreCache blockStoreCache, StoreBlockPuller blockPuller, BlockStoreLoop blockStoreLoop, BlockStoreManager blockStoreManager,
			BlockStoreSignaled blockStoreSignaled, FullNode.CancellationProvider cancellationProvider, NodeSettings nodeSettings)
		{
			this.chain = chain;
			this.signals = signals;
			this.blockRepository = blockRepository;
			this.blockStoreCache = blockStoreCache;
			this.blockPuller = blockPuller;
			this.blockStoreLoop = blockStoreLoop;
			this.blockStoreManager = blockStoreManager;
			this.blockStoreSignaled = blockStoreSignaled;
			this.cancellationProvider = cancellationProvider;
			this.connectionManager = connectionManager;
			this.nodeSettings = nodeSettings;
		}

		public override void Start()
		{
			this.connectionManager.Parameters.TemplateBehaviors.Add(new BlockStoreBehavior(this.chain, this.blockRepository, this.blockStoreCache));
			this.connectionManager.Parameters.TemplateBehaviors.Add(new BlockPuller.BlockPullerBehavior(this.blockPuller));
			this.connectionManager.Parameters.Services = (nodeSettings.Store.Prune ? NodeServices.Nothing : NodeServices.Network) | NodeServices.NODE_WITNESS;
			this.signals.Blocks.Subscribe(this.blockStoreSignaled);

			this.blockRepository.Initialize().GetAwaiter().GetResult();
			this.blockStoreSignaled.RelayWorker(this.cancellationProvider.Cancellation.Token);
			this.blockStoreLoop.Initialize(this.cancellationProvider.Cancellation).GetAwaiter().GetResult();			
		}

		public override void Stop()
		{
			Logs.BlockStore.LogInformation("Flushing BlockStore...");
			this.blockStoreManager.BlockStoreLoop.Flush().GetAwaiter().GetResult();

			this.blockStoreCache.Dispose();
			this.blockRepository.Dispose();
		}
    }

	public static class BlockStoreBuilderExtension
	{
		public static IFullNodeBuilder UseBlockStore(this IFullNodeBuilder fullNodeBuilder)
		{          
            fullNodeBuilder.ConfigureFeature(features =>
			{
				features
				.AddFeature<BlockStoreFeature>()
				.FeatureServices(services =>
					{
						services.AddSingleton<BlockRepository>();
						services.AddSingleton<BlockStoreCache>();
						services.AddSingleton<StoreBlockPuller>();
						services.AddSingleton<BlockStoreLoop>();
						services.AddSingleton<BlockStoreManager>();
						services.AddSingleton<BlockStoreSignaled>();
					});
			});

			return fullNodeBuilder;
		}
	}
}
