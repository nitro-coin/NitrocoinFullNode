﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.BitcoinCore;
using NBitcoin.Protocol;
using Nitrocoin.Bitcoin.BlockStore;
using Nitrocoin.Bitcoin.Connection;
using Nitrocoin.Bitcoin.Consensus;
using Nitrocoin.Bitcoin.MemoryPool;
using Xunit;

namespace Nitrocoin.Bitcoin.IntegrationTests
{
    public class BlockStoreTests
    {

		//[Fact]
		public void BlockRepositoryBench()
		{
			using (var dir = TestDirectory.Create())
			{
				using (var blockRepo = new BlockStore.BlockRepository(Network.Main, dir.FolderName))
				{
					var lst = new List<Block>();
					for (int i = 0; i < 30; i++)
					{
						// roughly 1mb blocks
						var block = new Block();
						for (int j = 0; j < 3000; j++)
						{
							var trx = new Transaction();
							block.AddTransaction(new Transaction());
							trx.AddInput(new TxIn(Script.Empty));
							trx.AddOutput(Money.COIN + j + i, new Script(Guid.NewGuid().ToByteArray()
								.Concat(Guid.NewGuid().ToByteArray())
								.Concat(Guid.NewGuid().ToByteArray())
								.Concat(Guid.NewGuid().ToByteArray())
								.Concat(Guid.NewGuid().ToByteArray())
								.Concat(Guid.NewGuid().ToByteArray())));
							trx.AddInput(new TxIn(Script.Empty));
							trx.AddOutput(Money.COIN + j + i + 1, new Script(Guid.NewGuid().ToByteArray()
								.Concat(Guid.NewGuid().ToByteArray())
								.Concat(Guid.NewGuid().ToByteArray())
								.Concat(Guid.NewGuid().ToByteArray())
								.Concat(Guid.NewGuid().ToByteArray())
								.Concat(Guid.NewGuid().ToByteArray())));
							block.AddTransaction(trx);
						}
						block.UpdateMerkleRoot();
						block.Header.HashPrevBlock = lst.Any() ? lst.Last().GetHash() : Network.Main.GenesisHash;
						lst.Add(block);
					}

					Stopwatch stopwatch = new Stopwatch();
					stopwatch.Start();
					blockRepo.PutAsync(lst.Last().GetHash(), lst).GetAwaiter().GetResult();
					var first = stopwatch.ElapsedMilliseconds;
					blockRepo.PutAsync(lst.Last().GetHash(), lst).GetAwaiter().GetResult();
					var second = stopwatch.ElapsedMilliseconds;

				}
			}
		}

		[Fact]
		public void BlockRepositoryPutBatch()
	    {
			using (var dir = TestDirectory.Create())
			{
				using (var blockRepo = new BlockStore.BlockRepository(Network.Main, dir.FolderName))
				{
					blockRepo.SetTxIndex(true).Wait();

					var lst = new List<Block>();
					for (int i = 0; i < 5; i++)
					{
						// put
						var block = new Block();
						block.AddTransaction(new Transaction());
						block.AddTransaction(new Transaction());
						block.Transactions[0].AddInput(new TxIn(Script.Empty));
						block.Transactions[0].AddOutput(Money.COIN + i * 2, Script.Empty);
						block.Transactions[1].AddInput(new TxIn(Script.Empty));
						block.Transactions[1].AddOutput(Money.COIN + i * 2 + 1, Script.Empty);
						block.UpdateMerkleRoot();
						block.Header.HashPrevBlock = lst.Any() ? lst.Last().GetHash() : Network.Main.GenesisHash;
						lst.Add(block);
					}

					blockRepo.PutAsync(lst.Last().GetHash(), lst).GetAwaiter().GetResult();

					// check each block
					foreach (var block in lst)
					{
						var received = blockRepo.GetAsync(block.GetHash()).GetAwaiter().GetResult();
						Assert.True(block.ToBytes().SequenceEqual(received.ToBytes()));

						foreach (var transaction in block.Transactions)
						{
							var trx = blockRepo.GetTrxAsync(transaction.GetHash()).GetAwaiter().GetResult();
							Assert.True(trx.ToBytes().SequenceEqual(transaction.ToBytes()));
						}
					}

					// delete
					blockRepo.DeleteAsync(lst.ElementAt(2).GetHash(), new[] {lst.ElementAt(2).GetHash()}.ToList());
					var deleted = blockRepo.GetAsync(lst.ElementAt(2).GetHash()).GetAwaiter().GetResult();
					Assert.Null(deleted);
				}
			}
		}

		[Fact]
		public void BlockRepositoryBlockHash()
		{
			using (var dir = TestDirectory.Create())
			{
				using (var blockRepo = new BlockStore.BlockRepository(Network.Main, dir.FolderName))
				{
                    blockRepo.Initialize().GetAwaiter().GetResult();

                    Assert.Equal(Network.Main.GenesisHash, blockRepo.BlockHash);
					var hash = new Block().GetHash();
					blockRepo.SetBlockHash(hash).GetAwaiter().GetResult();
					Assert.Equal(hash, blockRepo.BlockHash);
				}
			}
		}

		[Fact]
		public void BlockBroadcastInv()
	    {
			using (NodeBuilder builder = NodeBuilder.Create())
			{
				var NitrocoinNodeSync = builder.CreateNitrocoinNode();
				var NitrocoinNode1 = builder.CreateNitrocoinNode();
				var NitrocoinNode2 = builder.CreateNitrocoinNode();
				builder.StartAll();
                NitrocoinNodeSync.NotInIBD();
                NitrocoinNode1.NotInIBD();
                NitrocoinNode2.NotInIBD();

                // generate blocks and wait for the downloader to pickup
                NitrocoinNodeSync.SetDummyMinerSecret(new BitcoinSecret(new Key(), NitrocoinNodeSync.FullNode.Network));
				NitrocoinNodeSync.GenerateNitrocoin(10); // coinbase maturity = 10
				// wait for block repo for block sync to work
                TestHelper.WaitLoop(() => NitrocoinNodeSync.FullNode.ConsensusLoop.Tip.HashBlock == NitrocoinNodeSync.FullNode.Chain.Tip.HashBlock);
                TestHelper.WaitLoop(() => NitrocoinNodeSync.FullNode.ChainBehaviorState.HighestValidatedPoW.HashBlock == NitrocoinNodeSync.FullNode.Chain.Tip.HashBlock);
                TestHelper.WaitLoop(() => NitrocoinNodeSync.FullNode.ChainBehaviorState.HighestPersistedBlock.HashBlock == NitrocoinNodeSync.FullNode.Chain.Tip.HashBlock);

                // sync both nodes
                NitrocoinNode1.CreateRPCClient().AddNode(NitrocoinNodeSync.Endpoint, true);
				NitrocoinNode2.CreateRPCClient().AddNode(NitrocoinNodeSync.Endpoint, true);
				TestHelper.WaitLoop(() => NitrocoinNode1.CreateRPCClient().GetBestBlockHash() == NitrocoinNodeSync.CreateRPCClient().GetBestBlockHash());
				TestHelper.WaitLoop(() => NitrocoinNode2.CreateRPCClient().GetBestBlockHash() == NitrocoinNodeSync.CreateRPCClient().GetBestBlockHash());

				// set node2 to use inv (not headers)
				NitrocoinNode2.FullNode.ConnectionManager.ConnectedNodes.First().Behavior<BlockStoreBehavior>().PreferHeaders = false;

				// generate two new blocks
				NitrocoinNodeSync.GenerateNitrocoin(2);
				// wait for block repo for block sync to work
				TestHelper.WaitLoop(() => NitrocoinNodeSync.FullNode.Chain.Tip.HashBlock == NitrocoinNodeSync.FullNode.ConsensusLoop.Tip.HashBlock);
				TestHelper.WaitLoop(() => NitrocoinNodeSync.FullNode.BlockStoreManager.BlockRepository.GetAsync(NitrocoinNodeSync.CreateRPCClient().GetBestBlockHash()).Result != null);

				// wait for the other nodes to pick up the newly generated blocks
				TestHelper.WaitLoop(() => NitrocoinNode1.CreateRPCClient().GetBestBlockHash() == NitrocoinNodeSync.CreateRPCClient().GetBestBlockHash());
				TestHelper.WaitLoop(() => NitrocoinNode2.CreateRPCClient().GetBestBlockHash() == NitrocoinNodeSync.CreateRPCClient().GetBestBlockHash());
			}
		}

        [Fact]
        public void BlockStoreCanRecoverOnStartup()
        {
            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var NitrocoinNodeSync = builder.CreateNitrocoinNode();
                builder.StartAll();
                NitrocoinNodeSync.NotInIBD();

                // generate blocks and wait for the downloader to pickup
                NitrocoinNodeSync.SetDummyMinerSecret(new BitcoinSecret(new Key(), NitrocoinNodeSync.FullNode.Network));

                NitrocoinNodeSync.GenerateNitrocoin(10);
                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(NitrocoinNodeSync));

                // set the tip of best chain some blocks in the apst
                NitrocoinNodeSync.FullNode.Chain.SetTip(NitrocoinNodeSync.FullNode.Chain.GetBlock(NitrocoinNodeSync.FullNode.Chain.Height - 5));

                // stop the node it will persist the chain with the reset tip
                NitrocoinNodeSync.FullNode.Stop();

                var newNodeInstance = builder.CloneNitrocoinNode(NitrocoinNodeSync);

                // load the node, this should hit the block store recover code
                newNodeInstance.Start();

                // check that store recovered to be the same as the best chain.
               Assert.Equal(newNodeInstance.FullNode.Chain.Tip.HashBlock, newNodeInstance.FullNode.ChainBehaviorState.HighestPersistedBlock.HashBlock);
                //TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(NitrocoinNodeSync));
            }
        }

        [Fact]
		public void BlockStoreCanReorg()
		{
			using (NodeBuilder builder = NodeBuilder.Create())
			{
				var NitrocoinNodeSync = builder.CreateNitrocoinNode();
				var NitrocoinNode1 = builder.CreateNitrocoinNode();
				var NitrocoinNode2 = builder.CreateNitrocoinNode();
				builder.StartAll();
				NitrocoinNodeSync.NotInIBD();
				NitrocoinNode1.NotInIBD();
				NitrocoinNode2.NotInIBD();

				// generate blocks and wait for the downloader to pickup
				NitrocoinNode1.SetDummyMinerSecret(new BitcoinSecret(new Key(), NitrocoinNodeSync.FullNode.Network));
				NitrocoinNode2.SetDummyMinerSecret(new BitcoinSecret(new Key(), NitrocoinNodeSync.FullNode.Network));
				// sync both nodes
				NitrocoinNodeSync.CreateRPCClient().AddNode(NitrocoinNode1.Endpoint, true);
				NitrocoinNodeSync.CreateRPCClient().AddNode(NitrocoinNode2.Endpoint, true);

				NitrocoinNode1.GenerateNitrocoin(10);
				TestHelper.WaitLoop(() => NitrocoinNode1.FullNode.ChainBehaviorState.HighestPersistedBlock.Height == 10);

				TestHelper.WaitLoop(() => NitrocoinNode1.FullNode.ChainBehaviorState.HighestPersistedBlock.HashBlock == NitrocoinNodeSync.FullNode.ChainBehaviorState.HighestPersistedBlock.HashBlock);
				TestHelper.WaitLoop(() => NitrocoinNode2.FullNode.ChainBehaviorState.HighestPersistedBlock.HashBlock == NitrocoinNodeSync.FullNode.ChainBehaviorState.HighestPersistedBlock.HashBlock);

				// remove node 2
				NitrocoinNodeSync.CreateRPCClient().RemoveNode(NitrocoinNode2.Endpoint);

				// mine some more with node 1
				NitrocoinNode1.GenerateNitrocoin(10);

				// wait for node 1 to sync
				TestHelper.WaitLoop(() => NitrocoinNode1.FullNode.ChainBehaviorState.HighestPersistedBlock.Height == 20);
				TestHelper.WaitLoop(() => NitrocoinNode1.FullNode.ChainBehaviorState.HighestPersistedBlock.HashBlock == NitrocoinNodeSync.FullNode.ChainBehaviorState.HighestPersistedBlock.HashBlock);

				// remove node 1
				NitrocoinNodeSync.CreateRPCClient().RemoveNode(NitrocoinNode1.Endpoint);

				// mine a higher chain with node2
				NitrocoinNode2.GenerateNitrocoin(20);
				TestHelper.WaitLoop(() => NitrocoinNode2.FullNode.ChainBehaviorState.HighestPersistedBlock.Height == 30);

				// add node2 
				NitrocoinNodeSync.CreateRPCClient().AddNode(NitrocoinNode2.Endpoint, true);

				// node2 should be synced
				TestHelper.WaitLoop(() => NitrocoinNode2.FullNode.ChainBehaviorState.HighestPersistedBlock.HashBlock == NitrocoinNodeSync.FullNode.ChainBehaviorState.HighestPersistedBlock.HashBlock);
			}
		}

		[Fact]
		public void BlockStoreIndexTx()
		{
			using (NodeBuilder builder = NodeBuilder.Create())
			{
				var NitrocoinNode1 = builder.CreateNitrocoinNode();
				var NitrocoinNode2 = builder.CreateNitrocoinNode();
				builder.StartAll();
				NitrocoinNode1.NotInIBD();
				NitrocoinNode2.NotInIBD();

				// generate blocks and wait for the downloader to pickup
				NitrocoinNode1.SetDummyMinerSecret(new BitcoinSecret(new Key(), NitrocoinNode1.FullNode.Network));
				NitrocoinNode2.SetDummyMinerSecret(new BitcoinSecret(new Key(), NitrocoinNode2.FullNode.Network));
				// sync both nodes
				NitrocoinNode1.CreateRPCClient().AddNode(NitrocoinNode2.Endpoint, true);
				NitrocoinNode1.GenerateNitrocoin(10);
				TestHelper.WaitLoop(() => NitrocoinNode1.FullNode.ChainBehaviorState.HighestPersistedBlock.Height == 10);
				TestHelper.WaitLoop(() => NitrocoinNode1.FullNode.ChainBehaviorState.HighestPersistedBlock.HashBlock == NitrocoinNode2.FullNode.ChainBehaviorState.HighestPersistedBlock.HashBlock);

				var bestBlock1 = NitrocoinNode1.FullNode.BlockStoreManager.BlockRepository.GetAsync(NitrocoinNode1.FullNode.Chain.Tip.HashBlock).Result;
				Assert.NotNull(bestBlock1);

				// get the block coinbase trx 
				var trx = NitrocoinNode2.FullNode.BlockStoreManager.BlockRepository.GetTrxAsync(bestBlock1.Transactions.First().GetHash()).Result;
				Assert.NotNull(trx);
				Assert.Equal(bestBlock1.Transactions.First().GetHash(), trx.GetHash());
			}
		}
	}
}