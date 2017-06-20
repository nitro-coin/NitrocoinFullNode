using Microsoft.Extensions.DependencyInjection;
using Nitrocoin.Bitcoin.Builder;
using Nitrocoin.Bitcoin.Builder.Feature;
using Nitrocoin.Bitcoin.Connection;

namespace Nitrocoin.Bitcoin.Notifications
{
    /// <summary>
    /// Feature enabling the broadcasting of transactions.
    /// </summary>
    public class TransactionNotificationFeature : FullNodeFeature
    {
        private readonly ConnectionManager connectionManager;
        private readonly TransactionReceiver transactionBehavior;

        public TransactionNotificationFeature(ConnectionManager connectionManager, TransactionReceiver transactionBehavior)
        {
            this.connectionManager = connectionManager;
            this.transactionBehavior = transactionBehavior;
        }

        public override void Start()
        {
            this.connectionManager.Parameters.TemplateBehaviors.Add(this.transactionBehavior);
        }
    }

    public static class TransactionNotificationFeatureExtension
    {
        public static IFullNodeBuilder UseTransactionNotification(this IFullNodeBuilder fullNodeBuilder)
        {
            fullNodeBuilder.ConfigureFeature(features =>
            {
                features
                .AddFeature<TransactionNotificationFeature>()
                .FeatureServices(services =>
                    {
                        services.AddSingleton<TransactionNotificationProgress>();
                        services.AddSingleton<TransactionNotification>();
                        services.AddSingleton<TransactionReceiver>();                       
                    });
            });

            return fullNodeBuilder;
        }
    }
}
