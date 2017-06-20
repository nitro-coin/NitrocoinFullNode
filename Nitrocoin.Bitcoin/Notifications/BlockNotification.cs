﻿using NBitcoin;
using Nitrocoin.Bitcoin.BlockPulling;
using System.Threading;
using System.Threading.Tasks;
using Nitrocoin.Bitcoin.Utilities;

namespace Nitrocoin.Bitcoin.Notifications
{
	/// <summary>
	/// Class used to broadcast about new blocks.
	/// </summary>
	public class BlockNotification
	{
		private readonly ISignals signals;
        private ChainedBlock tip;

		public BlockNotification(ConcurrentChain chain, ILookaheadBlockPuller puller, ISignals signals)
		{
			Guard.NotNull(chain, nameof(chain));
			Guard.NotNull(puller, nameof(puller));
			Guard.NotNull(signals, nameof(signals));

			this.Chain = chain;
			this.Puller = puller;
			this.signals = signals;
		}

		public ILookaheadBlockPuller Puller { get; }

		public ConcurrentChain Chain { get; }

		public uint256 StartHash { get; private set; }

		private bool reSync;

		public void SyncFrom(uint256 startHash)
		{
			if (this.StartHash != null)
			{
				this.reSync = true;
			    ChainedBlock startBlock = this.Chain.GetBlock(startHash);
			    if (startBlock != null)
			    {
			        // sets the location of the puller to the latest hash that was broadcasted
			        this.Puller.SetLocation(startBlock);
                    this.tip = startBlock;
                }
                
            }

			this.StartHash = startHash;
		}
		
		/// <summary>
		/// Notifies about blocks, starting from block with hash passed as parameter.
		/// </summary>
		/// <param name="cancellationToken">A cancellation token</param>
		public virtual void Notify(CancellationToken cancellationToken)
		{
			AsyncLoop.Run("block notifier", token =>
			{
				// if the StartHash hasn't been set yet
				if (this.StartHash == null)
				{
					return Task.CompletedTask;
				}

				// make sure the chain has been downloaded
				ChainedBlock startBlock = this.Chain.GetBlock(this.StartHash);
				if (startBlock == null)
				{
					return Task.CompletedTask;
				}

				// sets the location of the puller to the latest hash that was broadcasted
				this.Puller.SetLocation(startBlock);
                this.tip = startBlock;

				// send notifications for all the following blocks
				while (!this.reSync)
				{					
					var block = this.Puller.NextBlock(token);

					if (block != null)
					{
						// broadcast the block to the registered observers
						this.signals.Blocks.Broadcast(block);
                        this.tip = this.Chain.GetBlock(block.GetHash());
					}
					else
					{
                        // in reorg we reset the puller to the fork
                        // when a reorg happens the puller is pushed
                        // back and continues from the current fork

                        // find the fork
                        while (this.Chain.GetBlock(this.tip.HashBlock) == null)
                            this.tip = this.tip.Previous;

                        // set the puller to the fork location
                        this.Puller.SetLocation(this.tip);
					}
				}
				
				this.reSync = false;

				return Task.CompletedTask;
			}, cancellationToken);
		}		
	}
}
