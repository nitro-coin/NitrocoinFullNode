﻿using System;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NBitcoin;
using Nitrocoin.Bitcoin.Common.JsonErrors;
using Nitrocoin.Bitcoin.Configuration;
using Nitrocoin.Bitcoin.Connection;
using Nitrocoin.Bitcoin.Wallet;
using Nitrocoin.Bitcoin.Wallet.Controllers;
using Nitrocoin.Bitcoin.Wallet.Helpers;
using Nitrocoin.Bitcoin.Wallet.Models;
using Xunit;

namespace Nitrocoin.Bitcoin.Tests.Wallet
{
    public class ControllersTests : TestBase
    {
        [Fact]
        public void CreateWalletSuccessfullyReturnsMnemonic()
        {
            Mnemonic mnemonic = new Mnemonic(Wordlist.English, WordCount.Twelve);
            var mockWalletCreate = new Mock<IWalletManager>();
            mockWalletCreate.Setup(wallet => wallet.CreateWallet(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(mnemonic);

            string dir = AssureEmptyDir("TestData/ControllersTests/CreateWalletSuccessfullyReturnsMnemonic");
            var dataFolder = new DataFolder(new NodeSettings {DataDir = dir});

            var controller = new WalletController(mockWalletCreate.Object, new Mock<IWalletSyncManager>().Object, It.IsAny<ConnectionManager>(), Network.Main, new Mock<ConcurrentChain>().Object, dataFolder);

            // Act
            var result = controller.Create(new WalletCreationRequest
            {
                Name = "myName",
                FolderPath = "",
                Password = "",
                Network = ""
            });

            // Assert
            mockWalletCreate.VerifyAll();
            var viewResult = Assert.IsType<JsonResult>(result);
            Assert.Equal(mnemonic.ToString(), viewResult.Value);
            Assert.NotNull(result);
        }

        [Fact]
        public void LoadWalletSuccessfullyReturnsWalletModel()
        {
            Bitcoin.Wallet.Wallet wallet = new Bitcoin.Wallet.Wallet
            {
                Name = "myWallet",
                Network = WalletHelpers.GetNetwork("mainnet")
            };

            var mockWalletWrapper = new Mock<IWalletManager>();
            mockWalletWrapper.Setup(w => w.RecoverWallet(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), null)).Returns(wallet);
            string dir = AssureEmptyDir("TestData/ControllersTests/LoadWalletSuccessfullyReturnsWalletModel");
            var dataFolder = new DataFolder(new NodeSettings { DataDir = dir });

            var controller = new WalletController(mockWalletWrapper.Object, new Mock<IWalletSyncManager>().Object, It.IsAny<ConnectionManager>(), Network.Main, new Mock<ConcurrentChain>().Object, dataFolder);

            // Act
            var result = controller.Recover(new WalletRecoveryRequest
            {
                Name = "myWallet",
                FolderPath = "",
                Password = "",
                Network = "MainNet",
                Mnemonic = "mnemonic"
            });

            // Assert
            mockWalletWrapper.VerifyAll();
            var viewResult = Assert.IsType<OkResult>(result);            
            Assert.Equal(200, viewResult.StatusCode);
        }

        [Fact]
        public void RecoverWalletSuccessfullyReturnsWalletModel()
        {
            Bitcoin.Wallet.Wallet wallet = new Bitcoin.Wallet.Wallet
            {
                Name = "myWallet",
                Network = WalletHelpers.GetNetwork("mainnet")
            };
            var mockWalletWrapper = new Mock<IWalletManager>();
            mockWalletWrapper.Setup(w => w.LoadWallet(It.IsAny<string>(), It.IsAny<string>())).Returns(wallet);
            string dir = AssureEmptyDir("TestData/ControllersTests/RecoverWalletSuccessfullyReturnsWalletModel");
            var dataFolder = new DataFolder(new NodeSettings { DataDir = dir });

            var controller = new WalletController(mockWalletWrapper.Object, new Mock<IWalletSyncManager>().Object, It.IsAny<ConnectionManager>(), Network.Main, new Mock<ConcurrentChain>().Object, dataFolder);

            // Act
            var result = controller.Load(new WalletLoadRequest
            {
                Name = "myWallet",
                FolderPath = "",
                Password = ""
            });

            // Assert
            mockWalletWrapper.VerifyAll();
            var viewResult = Assert.IsType<OkResult>(result);
            Assert.Equal(200, viewResult.StatusCode);
        }

        [Fact]
        public void FileNotFoundExceptionandReturns404()
        {
            var mockWalletWrapper = new Mock<IWalletManager>();
            mockWalletWrapper.Setup(wallet => wallet.LoadWallet(It.IsAny<string>(), It.IsAny<string>())).Throws<FileNotFoundException>();
            string dir = AssureEmptyDir("TestData/ControllersTests/FileNotFoundExceptionandReturns404");
            var dataFolder = new DataFolder(new NodeSettings { DataDir = dir });

            var controller = new WalletController(mockWalletWrapper.Object, new Mock<IWalletSyncManager>().Object, It.IsAny<ConnectionManager>(), Network.Main, new Mock<ConcurrentChain>().Object, dataFolder);

            // Act
            var result = controller.Load(new WalletLoadRequest
            {
                Name = "myName",
                FolderPath = "",
                Password = ""
            });

            // Assert
            mockWalletWrapper.VerifyAll();
            var viewResult = Assert.IsType<ErrorResult>(result);
            Assert.NotNull(viewResult);		
            Assert.Equal(404, viewResult.StatusCode);
        }        
    }
}
