﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nitrocoin.Bitcoin.Tests.Logging;
using Xunit;

namespace Nitrocoin.Bitcoin.Tests
{
    public class PeriodicTaskTest : LogsTestBase
    {
        private int iterationCount;

        /// <remarks>
        /// Unable to tests background thread exception throwing due to https://github.com/xunit/xunit/issues/157.
        /// </remarks>
        public PeriodicTaskTest() : base()
        {
            iterationCount = 0;
        }

        [Fact]
        public void StartLogsStartAndStop()
        {
            var periodicTask = new PeriodicTask("TestTask", async token =>
            {
                await DoTask(token);
            });

            periodicTask.Start(new CancellationTokenSource(100).Token, TimeSpan.FromMilliseconds(33));

            Thread.Sleep(120);            
            AssertLog(FullNodeLogger, LogLevel.Information, "TestTask starting");
            AssertLog(FullNodeLogger, LogLevel.Information, "TestTask stopping");
        }

        [Fact]
        public void StartWithoutDelayRunsTaskUntilCancelled()
        {
            var periodicTask = new PeriodicTask("TestTask", async token =>
            {
                await DoTask(token);
            });

            periodicTask.Start(new CancellationTokenSource(80).Token, TimeSpan.FromMilliseconds(33));

            Thread.Sleep(100);
            Assert.Equal(3, iterationCount);
        }

        [Fact]
        public void StartWithDelayUsesIntervalForDelayRunsTaskUntilCancelled()
        {
            var periodicTask = new PeriodicTask("TestTask", async token =>
            {
                await DoTask(token);
            });

            periodicTask.Start(new CancellationTokenSource(70).Token, TimeSpan.FromMilliseconds(33), true);

            Thread.Sleep(100);
            Assert.Equal(2, iterationCount);
        }

        [Fact]
        public void RunOnceDoesOneExecutionCycle()
        {
            var periodicTask = new PeriodicTask("TestTask", async token =>
            {
                await DoTask(token);
            });

            periodicTask.RunOnce();

            Assert.Equal(1, iterationCount);
        }

        private Task DoExceptionalTask(CancellationToken token)
        {
            Interlocked.Increment(ref iterationCount);

            if (iterationCount == 3)
            {
                throw new InvalidOperationException("Cannot run more than 3 times.");
            }

            return Task.CompletedTask;
        }


        private Task DoTask(CancellationToken token)
        {
            Interlocked.Increment(ref iterationCount);
            return Task.CompletedTask;
        }
    }
}