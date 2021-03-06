﻿using NBitcoin;

namespace Nitrocoin.Bitcoin.BlockPulling
{

	public class StoreBlockPuller : BlockPuller
	{
		public StoreBlockPuller(ConcurrentChain chain, Connection.ConnectionManager nodes) 
			: base(chain, nodes.ConnectedNodes, nodes.NodeSettings.ProtocolVersion)
		{
		}

		public void AskBlock(ChainedBlock downloadRequest)
		{
			base.AskBlocks(new ChainedBlock[] { downloadRequest });
		}

		public bool TryGetBlock(ChainedBlock chainedBlock, out DownloadedBlock block)
		{
			if (this.DownloadedBlocks.TryRemove(chainedBlock.HashBlock, out block))
			{
				return true;
			}

			this.OnStalling(chainedBlock);
			return false;
		}
	}

}
