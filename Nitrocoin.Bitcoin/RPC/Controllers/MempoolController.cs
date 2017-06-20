using Microsoft.AspNetCore.Mvc;
using NBitcoin;
using Nitrocoin.Bitcoin.MemoryPool;
using Nitrocoin.Bitcoin.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nitrocoin.Bitcoin.RPC.Controllers
{
    public class MempoolController : BaseRPCController
    {
        public MempoolController(MempoolManager mempoolManager) : base(mempoolManager: mempoolManager)
        {
            Guard.NotNull(this._MempoolManager, nameof(_MempoolManager));
        }

        [ActionName("getrawmempool")]
        public Task<List<uint256>> GetRawMempool()
        {
            return this._MempoolManager.GetMempoolAsync();
        }
    }
}
