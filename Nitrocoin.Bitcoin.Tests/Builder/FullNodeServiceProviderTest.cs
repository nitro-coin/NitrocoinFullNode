﻿using Moq;
using Nitrocoin.Bitcoin.Builder;
using Nitrocoin.Bitcoin.Builder.Feature;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Nitrocoin.Bitcoin.Tests.Builder
{
	public class FullNodeServiceProviderTest
	{
		private Mock<IServiceProvider> serviceProvider;

		public FullNodeServiceProviderTest()
		{
			this.serviceProvider = new Mock<IServiceProvider>();
			this.serviceProvider.Setup(c => c.GetService(typeof(TestFeatureStub)))
				.Returns(new TestFeatureStub());
			this.serviceProvider.Setup(c => c.GetService(typeof(TestFeatureStub2)))
				.Returns(new TestFeatureStub2());
		}

		[Fact]
		public void FeaturesReturnsFullNodeFeaturesFromServiceProvider()
		{
			var types = new List<Type> {
				typeof(TestFeatureStub),
				typeof(TestFeatureStub2)
			};

			var fullnodeServiceProvider = new FullNodeServiceProvider(this.serviceProvider.Object, types);
			var result = fullnodeServiceProvider.Features.ToList();

			Assert.Equal(2, result.Count);			
			Assert.Equal(typeof(TestFeatureStub), result[0].GetType());
			Assert.Equal(typeof(TestFeatureStub2), result[1].GetType());
		}

		[Fact]
		public void FeaturesReturnsInGivenOrder()
		{
			var types = new List<Type> {
				typeof(TestFeatureStub2),
				typeof(TestFeatureStub)

			};

			var fullnodeServiceProvider = new FullNodeServiceProvider(this.serviceProvider.Object, types);
			var result = fullnodeServiceProvider.Features.ToList();

			Assert.Equal(2, result.Count);
			Assert.Equal(typeof(TestFeatureStub2), result[0].GetType());
			Assert.Equal(typeof(TestFeatureStub), result[1].GetType());
		}

		private class TestFeatureStub : IFullNodeFeature
		{
			public void Start()
			{
				throw new NotImplementedException();
			}

			public void Stop()
			{
				throw new NotImplementedException();
			}
		}

		private class TestFeatureStub2 : IFullNodeFeature
		{
			public void Start()
			{
				throw new NotImplementedException();
			}

			public void Stop()
			{
				throw new NotImplementedException();
			}
		}
	}
}
