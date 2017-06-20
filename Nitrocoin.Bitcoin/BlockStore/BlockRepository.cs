﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin;
using Nitrocoin.Bitcoin.Configuration;
using Nitrocoin.Bitcoin.Utilities;

namespace Nitrocoin.Bitcoin.BlockStore
{
	public interface IBlockRepository : IDisposable
	{
		Task Initialize();

		Task PutAsync(uint256 nextBlockHash, List<Block> blocks);

		Task<Block> GetAsync(uint256 hash);

		Task<Transaction> GetTrxAsync(uint256 trxid);

		Task DeleteAsync(uint256 newlockHash, List<uint256> hashes);

		Task<bool> ExistAsync(uint256 hash);

		Task<uint256> GetTrxBlockIdAsync(uint256 trxid);

		Task SetBlockHash(uint256 nextBlockHash);

		Task SetTxIndex(bool txIndex);
	}

	public class BlockRepository : IBlockRepository
	{
		private readonly DBreezeSingleThreadSession session;
		private readonly Network network;
		private static readonly byte[] BlockHashKey = new byte[0];
		private static readonly byte[] TxIndexKey = new byte[1];
		public BlockStoreRepositoryPerformanceCounter PerformanceCounter { get; }

		public BlockRepository(Network network, DataFolder dataFolder)
			: this(network, dataFolder.BlockPath)
		{
		}

		public BlockRepository(Network network, string folder)
		{
			Guard.NotNull(network, nameof(network));
			Guard.NotEmpty(folder, nameof(folder));

			this.session = new DBreezeSingleThreadSession("DBreeze BlockRepository", folder);
			this.network = network;
			this.PerformanceCounter = new BlockStoreRepositoryPerformanceCounter();
		}

		public Task Initialize()
		{
			var genesis = this.network.GetGenesis();

			var sync = this.session.Do(() =>
			{
				this.session.Transaction.SynchronizeTables("Block", "Transaction", "Common");
				this.session.Transaction.ValuesLazyLoadingIsOn = true;
			});

			var hash = this.session.Do(() =>
			{
				if (this.LoadBlockHash() == null)
				{
					this.SaveBlockHash(genesis.GetHash());
					this.session.Transaction.Commit();
				}
				if (this.LoadTxIndex() == null)
				{
					this.SaveTxIndex(false);
					this.session.Transaction.Commit();
				}
			});

			return Task.WhenAll(new[] { sync, hash });
		}

		public bool LazyLoadingOn
		{
			get { return this.session.Transaction.ValuesLazyLoadingIsOn; }
			set { this.session.Transaction.ValuesLazyLoadingIsOn = value; }
		}

		public Task<Transaction> GetTrxAsync(uint256 trxid)
		{
			Guard.NotNull(trxid, nameof(trxid));

			if (!this.TxIndex)
				return Task.FromResult(default(Transaction));

			return this.session.Do(() =>
			{
				var blockid = this.session.Transaction.Select<byte[], uint256>("Transaction", trxid.ToBytes());
                if (!blockid.Exists)
                {
                    this.PerformanceCounter.AddRepositoryMissCount(1);
                    return null;
                }

                this.PerformanceCounter.AddRepositoryHitCount(1);
                var block = this.session.Transaction.Select<byte[], Block>("Block", blockid.Value.ToBytes());
				var trx = block?.Value?.Transactions.FirstOrDefault(t => t.GetHash() == trxid);

                if (trx == null)
                {
                    this.PerformanceCounter.AddRepositoryMissCount(1);
                }
                else
                {
                    this.PerformanceCounter.AddRepositoryHitCount(1);
                }

                return trx;
            });
		}

		public Task<uint256> GetTrxBlockIdAsync(uint256 trxid)
		{
			Guard.NotNull(trxid, nameof(trxid));

			if (!this.TxIndex)
				return Task.FromResult(default(uint256));

			return this.session.Do(() =>
			{
				var blockid = this.session.Transaction.Select<byte[], uint256>("Transaction", trxid.ToBytes());

                if (!blockid.Exists)
                {
                    this.PerformanceCounter.AddRepositoryMissCount(1);
                    return null;
                }
                else
                {
                    this.PerformanceCounter.AddRepositoryHitCount(1);
                    return blockid.Value;
                }
			});
		}		

		public uint256 BlockHash { get; private set; }
		public bool TxIndex { get; private set; }

		public Task PutAsync(uint256 nextBlockHash, List<Block> blocks)
		{
			Guard.NotNull(nextBlockHash, nameof(nextBlockHash));
			Guard.NotNull(blocks, nameof(blocks));

			// dbreeze is faster if sort ascending by key in memory before insert
			// however we need to find how byte arrays are sorted in dbreeze this link can help 
			// https://docs.google.com/document/pub?id=1IFkXoX3Tc2zHNAQN9EmGSXZGbabMrWmpmVxFsLxLsw

			return this.session.Do(() =>
			{
				foreach (var block in blocks)
				{
					var blockId = block.GetHash();

					// if the block is already in store don't write it again
					var item = this.session.Transaction.Select<byte[], Block>("Block", blockId.ToBytes());
                    if (!item.Exists)
                    {
                        this.PerformanceCounter.AddRepositoryMissCount(1);
                        this.PerformanceCounter.AddRepositoryInsertCount(1);
                        this.session.Transaction.Insert<byte[], Block>("Block", blockId.ToBytes(), block);

                        if (this.TxIndex)
                        {
                            // index transactions
                            foreach (var transaction in block.Transactions)
                            {
                                var trxId = transaction.GetHash();
                                this.PerformanceCounter.AddRepositoryInsertCount(1);
                                this.session.Transaction.Insert<byte[], uint256>("Transaction", trxId.ToBytes(), blockId);
                            }
                        }
                    }
                    else
                    {
                        this.PerformanceCounter.AddRepositoryHitCount(1);
                    }
                }

				this.SaveBlockHash(nextBlockHash);
				this.session.Transaction.Commit();
			});
		}

		private bool? LoadTxIndex()
		{
			var item = this.session.Transaction.Select<byte[], bool>("Common", TxIndexKey);

            if (!item.Exists)
            {
                this.PerformanceCounter.AddRepositoryMissCount(1);
                return null;
            }
            else
            {
                this.PerformanceCounter.AddRepositoryHitCount(1);
                this.TxIndex = item.Value;
                return item.Value;
            }
		}
		private void SaveTxIndex(bool txIndex)
		{
			this.TxIndex = txIndex;
            this.PerformanceCounter.AddRepositoryInsertCount(1);
			this.session.Transaction.Insert<byte[], bool>("Common", TxIndexKey, txIndex);
		}

		public Task SetTxIndex(bool txIndex)
		{
			return this.session.Do(() =>
			{
				this.SaveTxIndex(txIndex);
				this.session.Transaction.Commit();
			});
		}

		private uint256 LoadBlockHash()
		{
			this.BlockHash = this.BlockHash ?? this.session.Transaction.Select<byte[], uint256>("Common", BlockHashKey)?.Value;
			return this.BlockHash;
		}

		public Task SetBlockHash(uint256 nextBlockHash)
		{
			Guard.NotNull(nextBlockHash, nameof(nextBlockHash));

			return this.session.Do(() =>
			{
				this.SaveBlockHash(nextBlockHash);
				this.session.Transaction.Commit();
			});
		}

		private void SaveBlockHash(uint256 nextBlockHash)
		{
			this.BlockHash = nextBlockHash;
            this.PerformanceCounter.AddRepositoryInsertCount(1);
			this.session.Transaction.Insert<byte[], uint256>("Common", BlockHashKey, nextBlockHash);
		}

		public Task<Block> GetAsync(uint256 hash)
		{
			Guard.NotNull(hash, nameof(hash));

			return this.session.Do(() =>
			{
				var key = hash.ToBytes();                
                var item = this.session.Transaction.Select<byte[], Block>("Block", key);
                if (!item.Exists)
                {
                    this.PerformanceCounter.AddRepositoryMissCount(1);
                }
                else
                {
                    this.PerformanceCounter.AddRepositoryHitCount(1);
                }

                return item?.Value;                
			});
		}

		public Task<bool> ExistAsync(uint256 hash)
		{
			Guard.NotNull(hash, nameof(hash));

			return this.session.Do(() =>
			{
				var key = hash.ToBytes();
				var item = this.session.Transaction.Select<byte[], Block>("Block", key);
                if (!item.Exists)
                {
                    this.PerformanceCounter.AddRepositoryMissCount(1);                    
                }
                else
                {
                    this.PerformanceCounter.AddRepositoryHitCount(1);                
                }

                return item.Exists; // lazy loading is on so we don't fetch the whole value, just the row.
            });
		}

		public Task DeleteAsync(uint256 newlockHash, List<uint256> hashes)
		{
			Guard.NotNull(newlockHash, nameof(newlockHash));
			Guard.NotNull(hashes, nameof(hashes));

			return this.session.Do(() =>
			{
				foreach (var hash in hashes)
				{
					// if the block is already in store don't write it again
					var key = hash.ToBytes();

					if (this.TxIndex)
					{
						var block = this.session.Transaction.Select<byte[], Block>("Block", key);
                        if (block.Exists)
                        {
                            this.PerformanceCounter.AddRepositoryHitCount(1);

                            foreach (var transaction in block.Value.Transactions)
                            {
                                this.PerformanceCounter.AddRepositoryDeleteCount(1);
                                this.session.Transaction.RemoveKey<byte[]>("Transaction", transaction.GetHash().ToBytes());
                            }
                        }
                        else {
                            this.PerformanceCounter.AddRepositoryMissCount(1);
                        }
					}

                    this.PerformanceCounter.AddRepositoryDeleteCount(1);
                    this.session.Transaction.RemoveKey<byte[]>("Block", key);
				}

				this.SaveBlockHash(newlockHash);
				this.session.Transaction.Commit();
			});
		}

		public void Dispose()
		{
			this.session.Dispose();
		}
	}
}
