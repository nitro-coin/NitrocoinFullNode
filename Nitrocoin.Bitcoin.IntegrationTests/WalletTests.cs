using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NBitcoin;
using Nitrocoin.Bitcoin.BlockStore;
using Nitrocoin.Bitcoin.Connection;
using Nitrocoin.Bitcoin.Consensus;
using Nitrocoin.Bitcoin.Wallet;
using Xunit;
using Microsoft.Extensions.DependencyInjection;

namespace Nitrocoin.Bitcoin.IntegrationTests
{
    public class WalletTests
    {
        [Fact]
        public void WalletCanReceiveAndSendCorrectly()
        {
            using (NodeBuilder builder = NodeBuilder.Create())
            {
				var NitrocoinSender = builder.CreateNitrocoinNode();
				var NitrocoinReceiver = builder.CreateNitrocoinNode();

				builder.StartAll();
				NitrocoinSender.NotInIBD();
				NitrocoinReceiver.NotInIBD();

                // get a key from the wallet
                var mnemonic1 = NitrocoinSender.FullNode.WalletManager.CreateWallet("123456", "mywallet");
                var mnemonic2 = NitrocoinReceiver.FullNode.WalletManager.CreateWallet("123456", "mywallet");
                Assert.Equal(12, mnemonic1.Words.Length);
                Assert.Equal(12, mnemonic2.Words.Length);
                var addr = NitrocoinSender.FullNode.WalletManager.GetUnusedAddress("mywallet", "account 0");
                var key = NitrocoinSender.FullNode.WalletManager.GetKeyForAddress("123456", addr).PrivateKey;

                NitrocoinSender.SetDummyMinerSecret(new BitcoinSecret(key, NitrocoinSender.FullNode.Network));
                var maturity = (int)NitrocoinSender.FullNode.Network.Consensus.Option<PowConsensusOptions>().COINBASE_MATURITY;
                NitrocoinSender.GenerateNitrocoin(maturity + 5);
                // wait for block repo for block sync to work

                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(NitrocoinSender));

                // the mining should add coins to the wallet
                var total = NitrocoinSender.FullNode.WalletManager.GetSpendableTransactions().SelectMany(s => s.Transactions).Sum(s => s.Amount);
				Assert.Equal(Money.COIN * 105 * 50, total);

				// sync both nodes
				NitrocoinSender.CreateRPCClient().AddNode(NitrocoinReceiver.Endpoint, true);
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(NitrocoinReceiver, NitrocoinSender));

                // send coins to the receiver
                var sendto = NitrocoinReceiver.FullNode.WalletManager.GetUnusedAddress("mywallet", "account 0");
	            var trx = NitrocoinSender.FullNode.WalletManager.BuildTransaction("mywallet", "account 0", "123456", sendto.Address, Money.COIN * 100, string.Empty, 101);

				// broadcast to the other node
	            NitrocoinSender.FullNode.WalletManager.SendTransaction(trx.hex);

				// wait for the trx to arrive
	            TestHelper.WaitLoop(() => NitrocoinReceiver.CreateRPCClient().GetRawMempool().Length > 0);
	            TestHelper.WaitLoop(() => NitrocoinReceiver.FullNode.WalletManager.GetSpendableTransactions().SelectMany(s => s.Transactions).Any());

				var receivetotal = NitrocoinReceiver.FullNode.WalletManager.GetSpendableTransactions().SelectMany(s => s.Transactions).Sum(s => s.Amount);
	            Assert.Equal(Money.COIN * 100, receivetotal);
	            Assert.Null(NitrocoinReceiver.FullNode.WalletManager.GetSpendableTransactions().SelectMany(s => s.Transactions).First().BlockHeight);

				// generate two new blocks do the trx is confirmed
	            NitrocoinSender.GenerateNitrocoin(1, new List<Transaction>(new[] {new Transaction(trx.hex)}));
                NitrocoinSender.GenerateNitrocoin(1);

                // wait for block repo for block sync to work
                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(NitrocoinSender));
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(NitrocoinReceiver, NitrocoinSender));

                TestHelper.WaitLoop(() => maturity + 6 == NitrocoinReceiver.FullNode.WalletManager.GetSpendableTransactions().SelectMany(s => s.Transactions).First().BlockHeight);
            }

        }

        [Fact]
        public void WalletCanReorg()
        {
            // this test has 4 parts:
            // send first transaction from one wallet to another and wait for it to be confirmed
            // send a second transaction and wait for it to be confirmed
            // connected to a longer chain that couse a reorg back so the second trasnaction is undone
            // mine the second transaction back in to the main chain

            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var NitrocoinSender = builder.CreateNitrocoinNode();
                var NitrocoinReceiver = builder.CreateNitrocoinNode();
                var NitrocoinReorg = builder.CreateNitrocoinNode();

                builder.StartAll();
                NitrocoinSender.NotInIBD();
                NitrocoinReceiver.NotInIBD();
                NitrocoinReorg.NotInIBD();

                // get a key from the wallet
                var mnemonic1 = NitrocoinSender.FullNode.WalletManager.CreateWallet("123456", "mywallet");
                var mnemonic2 = NitrocoinReceiver.FullNode.WalletManager.CreateWallet("123456", "mywallet");
                Assert.Equal(12, mnemonic1.Words.Length);
                Assert.Equal(12, mnemonic2.Words.Length);
                var addr = NitrocoinSender.FullNode.WalletManager.GetUnusedAddress("mywallet", "account 0");
                var key = NitrocoinSender.FullNode.WalletManager.GetKeyForAddress("123456", addr).PrivateKey;

                NitrocoinSender.SetDummyMinerSecret(new BitcoinSecret(key, NitrocoinSender.FullNode.Network));
                NitrocoinReorg.SetDummyMinerSecret(new BitcoinSecret(key, NitrocoinSender.FullNode.Network));

                var maturity = (int)NitrocoinSender.FullNode.Network.Consensus.Option<PowConsensusOptions>().COINBASE_MATURITY;
                NitrocoinSender.GenerateNitrocoin(maturity + 15);
                var currentBestHeight = maturity + 15;

                // wait for block repo for block sync to work
                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(NitrocoinSender));

                // the mining should add coins to the wallet
                var total = NitrocoinSender.FullNode.WalletManager.GetSpendableTransactions().SelectMany(s => s.Transactions).Sum(s => s.Amount);
                Assert.Equal(Money.COIN * currentBestHeight * 50, total);

                // sync all nodes
                NitrocoinReceiver.CreateRPCClient().AddNode(NitrocoinSender.Endpoint, true);
                NitrocoinReceiver.CreateRPCClient().AddNode(NitrocoinReorg.Endpoint, true);
                NitrocoinSender.CreateRPCClient().AddNode(NitrocoinReorg.Endpoint, true);
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(NitrocoinReceiver, NitrocoinSender));
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(NitrocoinReceiver, NitrocoinReorg));

                // Build Transaction 1
                // ====================
                // send coins to the receiver
                var sendto = NitrocoinReceiver.FullNode.WalletManager.GetUnusedAddress("mywallet", "account 0");
                var transaction1 = NitrocoinSender.FullNode.WalletManager.BuildTransaction("mywallet", "account 0", "123456", sendto.Address, Money.COIN * 100, string.Empty, 101);

                // broadcast to the other node
                NitrocoinSender.FullNode.WalletManager.SendTransaction(transaction1.hex);

                // wait for the trx to arrive
                TestHelper.WaitLoop(() => NitrocoinReceiver.CreateRPCClient().GetRawMempool().Length > 0);
                Assert.NotNull(NitrocoinReceiver.CreateRPCClient().GetRawTransaction(transaction1.transactionId, false));
                TestHelper.WaitLoop(() => NitrocoinReceiver.FullNode.WalletManager.GetSpendableTransactions().SelectMany(s => s.Transactions).Any());

                var receivetotal = NitrocoinReceiver.FullNode.WalletManager.GetSpendableTransactions().SelectMany(s => s.Transactions).Sum(s => s.Amount);
                Assert.Equal(Money.COIN * 100, receivetotal);
                Assert.Null(NitrocoinReceiver.FullNode.WalletManager.GetSpendableTransactions().SelectMany(s => s.Transactions).First().BlockHeight);

                // generate two new blocks so the trx is confirmed
                NitrocoinSender.GenerateNitrocoin(1, new List<Transaction>(new[] { new Transaction(transaction1.hex) }));
                var transaction1MinedHeight = currentBestHeight + 1;
                NitrocoinSender.GenerateNitrocoin(1);
                currentBestHeight = currentBestHeight + 2;

                // wait for block repo for block sync to work
                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(NitrocoinSender));
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(NitrocoinReceiver, NitrocoinSender));
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(NitrocoinReceiver, NitrocoinReorg));
                Assert.Equal(currentBestHeight, NitrocoinReceiver.FullNode.Chain.Tip.Height);
                TestHelper.WaitLoop(() => transaction1MinedHeight == NitrocoinReceiver.FullNode.WalletManager.GetSpendableTransactions().SelectMany(s => s.Transactions).First().BlockHeight);

                // Build Transaction 2
                // ====================
                // remove the reorg node
                NitrocoinReceiver.CreateRPCClient().RemoveNode(NitrocoinReorg.Endpoint);
                NitrocoinSender.CreateRPCClient().RemoveNode(NitrocoinReorg.Endpoint);
                var forkblock = NitrocoinReceiver.FullNode.Chain.Tip;

                // send more coins to the wallet
                sendto = NitrocoinReceiver.FullNode.WalletManager.GetUnusedAddress("mywallet", "account 0");
                var transaction2 = NitrocoinSender.FullNode.WalletManager.BuildTransaction("mywallet", "account 0", "123456", sendto.Address, Money.COIN * 10, string.Empty, 101);
                NitrocoinSender.FullNode.WalletManager.SendTransaction(transaction2.hex);
                // wait for the trx to arrive
                TestHelper.WaitLoop(() => NitrocoinReceiver.CreateRPCClient().GetRawMempool().Length > 0);
                Assert.NotNull(NitrocoinReceiver.CreateRPCClient().GetRawTransaction(transaction2.transactionId, false));
                TestHelper.WaitLoop(() => NitrocoinReceiver.FullNode.WalletManager.GetSpendableTransactions().SelectMany(s => s.Transactions).Any());
                var newamount = NitrocoinReceiver.FullNode.WalletManager.GetSpendableTransactions().SelectMany(s => s.Transactions).Sum(s => s.Amount);
                Assert.Equal(Money.COIN * 110, newamount);
                Assert.True(NitrocoinReceiver.FullNode.WalletManager.GetSpendableTransactions().SelectMany(s => s.Transactions).Any(b => b.BlockHeight == null));

                // mine more blocks so its included in the chain
              
                NitrocoinSender.GenerateNitrocoin(1, new List<Transaction>(new[] { new Transaction(transaction2.hex) }));
                var transaction2MinedHeight = currentBestHeight + 1;
                NitrocoinSender.GenerateNitrocoin(1);
                currentBestHeight = currentBestHeight + 2;
                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(NitrocoinSender));
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(NitrocoinReceiver, NitrocoinSender));
                Assert.Equal(currentBestHeight, NitrocoinReceiver.FullNode.Chain.Tip.Height);
                TestHelper.WaitLoop(() => NitrocoinReceiver.FullNode.WalletManager.GetSpendableTransactions().SelectMany(s => s.Transactions).Any(b => b.BlockHeight == transaction2MinedHeight));

                // create a reog by mining on two different chains
                // ================================================
                // advance both chains, one chin is longer
                NitrocoinSender.GenerateNitrocoin(2);
                NitrocoinReorg.GenerateNitrocoin(10);
                currentBestHeight = forkblock.Height + 10;
                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(NitrocoinSender));
                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(NitrocoinReorg));

                // connect the reorg chain
                NitrocoinReceiver.CreateRPCClient().AddNode(NitrocoinReorg.Endpoint, true);
                NitrocoinSender.CreateRPCClient().AddNode(NitrocoinReorg.Endpoint, true);
                // wait for the chains to catch up
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(NitrocoinReceiver, NitrocoinSender));
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(NitrocoinReceiver, NitrocoinReorg));
                Assert.Equal(currentBestHeight, NitrocoinReceiver.FullNode.Chain.Tip.Height);

                // ensure wallet reorg complete
                TestHelper.WaitLoop(() => NitrocoinReceiver.FullNode.WalletManager.WalletTipHash == NitrocoinReorg.CreateRPCClient().GetBestBlockHash());
                // check the wallet amont was roled back
                var newtotal = NitrocoinReceiver.FullNode.WalletManager.GetSpendableTransactions().SelectMany(s => s.Transactions).Sum(s => s.Amount);
                Assert.Equal(receivetotal, newtotal);
                TestHelper.WaitLoop(() => maturity + 16 == NitrocoinReceiver.FullNode.WalletManager.GetSpendableTransactions().SelectMany(s => s.Transactions).First().BlockHeight);

                // ReBuild Transaction 2
                // ====================
                // mine the transaction again
                NitrocoinSender.GenerateNitrocoin(1, new List<Transaction>(new[] { new Transaction(transaction2.hex) }));
                transaction2MinedHeight = currentBestHeight + 1;
                NitrocoinSender.GenerateNitrocoin(1);
                currentBestHeight = currentBestHeight + 2;

                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(NitrocoinSender));
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(NitrocoinReceiver, NitrocoinSender));
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(NitrocoinReceiver, NitrocoinReorg));
                Assert.Equal(currentBestHeight, NitrocoinReceiver.FullNode.Chain.Tip.Height);
                var newsecondamount = NitrocoinReceiver.FullNode.WalletManager.GetSpendableTransactions().SelectMany(s => s.Transactions).Sum(s => s.Amount);
                Assert.Equal(newamount, newsecondamount);
                TestHelper.WaitLoop(() => NitrocoinReceiver.FullNode.WalletManager.GetSpendableTransactions().SelectMany(s => s.Transactions).Any(b => b.BlockHeight == transaction2MinedHeight));
            }
        }

        [Fact]
        public void WalletCanCatchupWithBestChain()
        {
            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var Nitrocoinminer = builder.CreateNitrocoinNode();

                builder.StartAll();
                Nitrocoinminer.NotInIBD();

                // get a key from the wallet
                var mnemonic = Nitrocoinminer.FullNode.WalletManager.CreateWallet("123456", "mywallet");
                Assert.Equal(12, mnemonic.Words.Length);
                var addr = Nitrocoinminer.FullNode.WalletManager.GetUnusedAddress("mywallet", "account 0");
                var key = Nitrocoinminer.FullNode.WalletManager.GetKeyForAddress("123456", addr).PrivateKey;

                Nitrocoinminer.SetDummyMinerSecret(key.GetBitcoinSecret(Nitrocoinminer.FullNode.Network));
                Nitrocoinminer.GenerateNitrocoin(10);
                // wait for block repo for block sync to work
                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(Nitrocoinminer));

                // push the wallet back
                Nitrocoinminer.FullNode.Services.ServiceProvider.GetService<IWalletSyncManager>().SyncFrom(5);

                Nitrocoinminer.GenerateNitrocoin(5);

                TestHelper.WaitLoop(() => TestHelper.IsNodeSynced(Nitrocoinminer));
            }
        }

        [Fact]
        public void WalletCanRecoverOnStartup()
        {
            using (NodeBuilder builder = NodeBuilder.Create())
            {
                var NitrocoinNodeSync = builder.CreateNitrocoinNode();
                builder.StartAll();
                NitrocoinNodeSync.NotInIBD();

                // get a key from the wallet
                var mnemonic = NitrocoinNodeSync.FullNode.WalletManager.CreateWallet("123456", "mywallet");
                Assert.Equal(12, mnemonic.Words.Length);
                var addr = NitrocoinNodeSync.FullNode.WalletManager.GetUnusedAddress("mywallet", "account 0");
                var key = NitrocoinNodeSync.FullNode.WalletManager.GetKeyForAddress("123456", addr).PrivateKey;

                NitrocoinNodeSync.SetDummyMinerSecret(key.GetBitcoinSecret(NitrocoinNodeSync.FullNode.Network));
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
                Assert.Equal(newNodeInstance.FullNode.Chain.Tip.HashBlock, newNodeInstance.FullNode.WalletManager.WalletTipHash);
            }
        }
    }
}
