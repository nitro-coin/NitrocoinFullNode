using System;

namespace Nitrocoin.Bitcoin.Miner
{
    public class MinerException : Exception
    {
		public MinerException(string message) : base(message)
		{ }
	}
}
