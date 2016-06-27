using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Http;
using Microsoft.ServiceFabric.Http.Client;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Application1.UserSessionService
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
            this.StartTrimmingData(cancellationToken);

            return base.RunAsync(cancellationToken);
        }
        #endregion StatefulService

        #region Data trimming
        private void StartTrimmingData(CancellationToken cancellationToken)
        {
            TimeSpan frequency = TimeSpan.FromSeconds(60);

            this.dataTrimmingTimer = new Timer(async (state) =>
            {
                var sessionDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, SessionData>>("SessionData");
                using (var tx = this.StateManager.CreateTransaction())
                {
                    var sessions = await sessionDictionary.CreateEnumerableAsync(tx);
                    using (var e = sessions.GetAsyncEnumerator())
                    {
                        while (await e.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                        {
                            if (DateTimeOffset.UtcNow.Subtract(e.Current.Value.LastAccessedOn).TotalHours > 1)
                            {
                                await sessionDictionary.TryRemoveAsync(tx, e.Current.Key);
                            }
                        }
                    }

                    await tx.CommitAsync();
                }
                this.dataTrimmingTimer.Change(frequency, Timeout.InfiniteTimeSpan);
            }, null, frequency, Timeout.InfiniteTimeSpan);
        }

        private Timer dataTrimmingTimer;
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
            var handler = new CircuitBreakerHttpMessageHandler(int.MaxValue, TimeSpan.Zero,
                            new HttpServiceClientHandler(
                                new HttpServiceClientExceptionHandler(
                                    new HttpServiceClientStatusCodeRetryHandler(
                                        new HttpTraceMessageHandler(this.Context, new HttpClientHandler())))));
            return new HttpClient(handler);
        }
        private Lazy<HttpClient> httpClient;
        #endregion
    }
}
