using Microsoft.AspNetCore.Mvc;
using NBitcoin;
using Nitrocoin.Bitcoin.BlockStore;
using Nitrocoin.Bitcoin.Builder;
using Nitrocoin.Bitcoin.Configuration;
using Nitrocoin.Bitcoin.Consensus;
using Nitrocoin.Bitcoin.MemoryPool;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nitrocoin.Bitcoin.RPC.Controllers
{
    public abstract class BaseRPCController : Controller
    {
        protected IFullNode _FullNode;
        protected NodeSettings _Settings;
        protected Network _Network;
        protected PowConsensusValidator _ConsensusValidator;
        protected ConsensusLoop _ConsensusLoop;
        protected ChainBase _Chain;
        protected ChainBehavior.ChainState _ChainState;
        protected BlockStoreManager _BlockManager;
        protected MempoolManager _MempoolManager;
        protected Connection.ConnectionManager _ConnectionManager;

        public BaseRPCController(
            IFullNode fullNode = null,
            NodeSettings nodeSettings = null,
            Network network = null,
            PowConsensusValidator consensusValidator = null,
            ConsensusLoop consensusLoop = null,
            ConcurrentChain chain = null,
            ChainBehavior.ChainState chainState = null,
            BlockStoreManager blockManager = null,
            MempoolManager mempoolManager = null,
            Connection.ConnectionManager connectionManager = null)
        {
            this._FullNode = fullNode;
            this._Settings = nodeSettings;
            this._Network = network;
            this._ConsensusValidator = consensusValidator;
            this._ConsensusLoop = consensusLoop;
            this._Chain = chain;
            this._ChainState = chainState;
            this._BlockManager = blockManager;
            this._MempoolManager = mempoolManager;
            this._ConnectionManager = connectionManager;
        }

    }
}
