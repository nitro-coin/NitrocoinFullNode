using NBitcoin;
using System.Threading;

namespace Nitrocoin.Bitcoin.BlockPulling
{
	public interface IBlockPuller
	{
		void AskBlocks(ChainedBlock[] downloadRequests);

		void PushBlock(int length, Block block, CancellationToken token);

		bool IsDownloading(uint256 hash);
	}
}
