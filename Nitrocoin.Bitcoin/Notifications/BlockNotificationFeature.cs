using Microsoft.Extensions.DependencyInjection;
using NBitcoin;
using Nitrocoin.Bitcoin.BlockPulling;
using Nitrocoin.Bitcoin.BlockStore;
using Nitrocoin.Bitcoin.Builder;
using Nitrocoin.Bitcoin.Builder.Feature;
using Nitrocoin.Bitcoin.Connection;

namespace Nitrocoin.Bitcoin.Notifications
{
	/// <summary>
	/// Feature enabling the broadcasting of blocks.
	/// </summary>
	public class BlockNotificationFeature : FullNodeFeature
	{
		private readonly BlockNotification blockNotification;
		private readonly FullNode.CancellationProvider cancellationProvider;
		private readonly ConnectionManager connectionManager;
		private readonly LookaheadBlockPuller blockPuller;
		private readonly ChainBehavior.ChainState chainState;
		private readonly ConcurrentChain chain;

		public BlockNotificationFeature(BlockNotification blockNotification, FullNode.CancellationProvider cancellationProvider, ConnectionManager connectionManager, LookaheadBlockPuller blockPuller, ChainBehavior.ChainState chainState, ConcurrentChain chain)
		{
			this.blockNotification = blockNotification;
			this.cancellationProvider = cancellationProvider;
			this.connectionManager = connectionManager;
			this.blockPuller = blockPuller;
			this.chainState = chainState;
			this.chain = chain;
		}

		public override void Start()
		{
			var connectionParameters = this.connectionManager.Parameters;
			connectionParameters.TemplateBehaviors.Add(new BlockPuller.BlockPullerBehavior(blockPuller));			
			this.blockNotification.Notify(this.cancellationProvider.Cancellation.Token);
			this.chainState.HighestValidatedPoW = this.chain.Genesis;
		}
	}

	public static class BlockNotificationFeatureExtension
	{
		public static IFullNodeBuilder UseBlockNotification(this IFullNodeBuilder fullNodeBuilder)
		{
			fullNodeBuilder.ConfigureFeature(features =>
			{
				features
				.AddFeature<BlockNotificationFeature>()
				.FeatureServices(services =>
				{					
					services.AddSingleton<BlockNotification>();				
					services.AddSingleton<LookaheadBlockPuller>().AddSingleton<ILookaheadBlockPuller, LookaheadBlockPuller>(provider => provider.GetService<LookaheadBlockPuller>());
				});
			});

			return fullNodeBuilder;
		}
	}
}
