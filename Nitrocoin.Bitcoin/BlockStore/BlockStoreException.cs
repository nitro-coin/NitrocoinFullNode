using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nitrocoin.Bitcoin.BlockStore
{
    public class BlockStoreException : Exception
    {
		public BlockStoreException(string message) : base(message)
		{ }
	}
}
