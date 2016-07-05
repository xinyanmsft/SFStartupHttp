using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Http;
using Microsoft.ServiceFabric.Http.Client;
using Microsoft.ServiceFabric.Http.Utilities;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Application1.ValuesService
{
    /// <summary>
    /// A specialized stateless service for hosting ASP.NET Core web apps.
    /// </summary>
    internal sealed class WebHostingService : StatefulService
    {
        public WebHostingService(StatefulServiceContext serviceContext, IReliableStateManagerReplica stateManager)
            : base(serviceContext, stateManager)
        {
            this.httpClient = new Lazy<HttpClient>(this.CreateHttpClient);
            this.serviceWatchdog = new Lazy<ServiceWatchdog>(() => new ServiceWatchdog(this.Context, this.Partition));
        }

        #region StatefulService
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new[]
            {
                new ServiceReplicaListener(context =>
                    new WebHostCommunicationListener(context, "ServiceEndpoint", uri =>
                        new WebHostBuilder().UseWebListener()
                                           .UseStartup<Startup>()
                                           .UseUrls(uri)
                                           .ConfigureServices(services => {
                                               services.AddSingleton<IReliableStateManager>(this.StateManager);
                                               services.AddSingleton<IServicePartition>(this.Partition);
                                               services.AddSingleton<ServiceWatchdog>(this.ServiceWatchdog);
                                               services.AddSingleton<ServiceContext>(this.Context);
                                               services.AddSingleton<HttpClient>(this.HttpClient);
                                           })
                                           .Build()), name: "web")
            };
        }

        protected override Task RunAsync(CancellationToken cancellationToken)
        {
            this.ServiceWatchdog.StartMonitoring(cancellationToken);
            Task.Run(() => this.TrimmingDataAsync(cancellationToken));

            return base.RunAsync(cancellationToken);
        }
        #endregion StatefulService

        #region Data trimming
        private async Task TrimmingDataAsync(CancellationToken cancellationToken)
        {
            while(!cancellationToken.IsCancellationRequested)
            {
                var entities = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, ValuesEntity>>("Values");
                await ExponentialBackoff.Run((Func<Task>)(async () =>
                {
                    using (var tx = this.StateManager.CreateTransaction())
                    {
                        List<string> dataToRemove = new List<string>();
                        var values = await entities.CreateEnumerableAsync(tx);
                        using (var e = values.GetAsyncEnumerator())
                        {
                            while (await e.MoveNextAsync(cancellationToken))
                            {
                                if (DateTimeOffset.UtcNow.Subtract((DateTimeOffset)e.Current.Value.LastAccessedOn).TotalHours > 1)
                                {
                                    dataToRemove.Add((string)e.Current.Key);
                                }
                            }
                        }
                        foreach (var s in dataToRemove)
                        {
                            await entities.TryRemoveAsync(tx, s, TimeSpan.FromSeconds(4), cancellationToken);
                        }
                        await tx.CommitAsync();
                    }
                }), cancellationToken);
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }
        }
        #endregion
        
        #region Provide ServiceWatchdog
        internal ServiceWatchdog ServiceWatchdog { get { return this.serviceWatchdog.Value; } }
        private Lazy<ServiceWatchdog> serviceWatchdog;
        #endregion

        #region Provide HttpClient
        internal HttpClient HttpClient { get { return this.httpClient.Value; } }

        private HttpClient CreateHttpClient()
        {
            // TODO: To enable circuit breaker pattern, set proper values in CircuitBreakerHttpMessageHandler constructor
            var handler = new CircuitBreakerHttpMessageHandler(10, TimeSpan.FromSeconds(10),
                            new HttpServiceClientHandler(
                                new HttpServiceClientExceptionHandler(
                                    new HttpServiceClientStatusCodeRetryHandler(
                                        new HttpTraceMessageHandler(this.Context)))));
            return new HttpClient(handler);
        }
        private Lazy<HttpClient> httpClient;
        #endregion
    }
}
