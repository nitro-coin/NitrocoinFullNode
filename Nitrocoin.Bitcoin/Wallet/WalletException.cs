﻿using System;

namespace Nitrocoin.Bitcoin.Wallet
{
    public class WalletException : Exception
    {
		public WalletException(string message) : base(message)
		{ }
	}
}
