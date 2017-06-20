﻿using NBitcoin;
using Nitrocoin.Bitcoin.Consensus;
using Nitrocoin.Bitcoin.BlockPulling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nitrocoin.Bitcoin.Utilities;

namespace Nitrocoin.Bitcoin.IntegrationTests
{
	public class ChainBuilder
	{
		ConcurrentChain _Chain = new ConcurrentChain();
		Network _Network;
		public ChainBuilder(Network network)
		{
			Guard.NotNull(network, nameof(network));
			
			_Network = network;
			_Chain = new ConcurrentChain(_Network);
			MinerKey = new Key();
			MinerScriptPubKey = MinerKey.PubKey.Hash.ScriptPubKey;
		}

		public ConcurrentChain Chain
		{
			get
			{
				return _Chain;
			}
		}

		public Key MinerKey
		{
			get;
			private set;
		}
		public Script MinerScriptPubKey
		{
			get;
			private set;
		}

		public Transaction Spend(ICoin[] coins, Money amount)
		{
			TransactionBuilder builder = new TransactionBuilder();
			builder.AddCoins(coins);
			builder.AddKeys(MinerKey);
			builder.Send(MinerScriptPubKey, amount);
			builder.SendFees(Money.Coins(0.01m));
			builder.SetChange(MinerScriptPubKey);
			var tx = builder.BuildTransaction(true);
			return tx;
		}

		public ICoin[] GetSpendableCoins()
		{
			return _Blocks
				.Select(b => b.Value)
				.SelectMany(b => b.Transactions.Select(t => new
				{
					Tx = t,
					Block = b
				}))
				.Where(b => !b.Tx.IsCoinBase || (_Chain.Height + 1) - _Chain.GetBlock(b.Block.GetHash()).Height >= 100)
				.Select(b => b.Tx)
				.SelectMany(b => b.Outputs.AsIndexedOutputs())
				.Where(o => o.TxOut.ScriptPubKey == this.MinerScriptPubKey)
				.Select(o => new Coin(o))
				.ToArray();
		}

		public void Mine(int blockCount)
		{
			List<Block> blocks = new List<Block>();
			DateTimeOffset now = DateTimeOffset.UtcNow;
			for(int i = 0; i < blockCount; i++)
			{
				uint nonce = 0;
				Block block = new Block();
				block.Header.HashPrevBlock = _Chain.Tip.HashBlock;
				block.Header.Bits = block.Header.GetWorkRequired(_Network, _Chain.Tip);
				block.Header.UpdateTime(now, _Network, _Chain.Tip);
				var coinbase = new Transaction();
				coinbase.AddInput(TxIn.CreateCoinbase(_Chain.Height + 1));
				coinbase.AddOutput(new TxOut(_Network.GetReward(_Chain.Height + 1), MinerScriptPubKey));
				block.AddTransaction(coinbase);
				foreach(var tx in _Transactions)
				{
					block.AddTransaction(tx);
				}
				block.UpdateMerkleRoot();
				while(!block.CheckProofOfWork())
					block.Header.Nonce = ++nonce;
				block.Header.CacheHashes();
				blocks.Add(block);
				_Transactions.Clear();
				_Chain.SetTip(block.Header);
			}

			foreach(var b in blocks)
			{
				_Blocks.Add(b.GetHash(), b);
			}
		}

		internal Dictionary<uint256, Block> _Blocks = new Dictionary<uint256, Block>();
		private List<Transaction> _Transactions = new List<Transaction>();
		public void Broadcast(Transaction tx)
		{
			_Transactions.Add(tx);
		}
	}
}
