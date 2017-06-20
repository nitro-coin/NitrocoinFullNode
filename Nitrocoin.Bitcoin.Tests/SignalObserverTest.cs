﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Internal;
using Moq;
using NBitcoin;
using Nitrocoin.Bitcoin;
using Nitrocoin.Bitcoin.Logging;
using Nitrocoin.Bitcoin.Tests.Logging;
using Xunit;

namespace Nitrocoin.Bitcoin.Tests
{
    public class SignalObserverTest : LogsTestBase
    {
        SignalObserver<Block> observer;               

        public SignalObserverTest() : base()
        {
            observer = new TestBlockSignalObserver();
        }

        [Fact]
        public void SignalObserverLogsSignalOnError()
        {
            var exception = new InvalidOperationException("This should not have occurred!");

            observer.OnError(exception);

            AssertLog(FullNodeLogger, LogLevel.Error, exception.ToString());
        }            

        private class TestBlockSignalObserver : SignalObserver<Block>
        {
            public TestBlockSignalObserver()
            {
            }

            protected override void OnNextCore(Block value)
            {

            }
        }
    }
}
