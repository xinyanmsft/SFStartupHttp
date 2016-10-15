﻿using Application1.ValuesService.Utility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Newtonsoft.Json;
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
            this.serviceMonitor = new Lazy<ServiceMonitor>(() => new ServiceMonitor(this.Context, this.Partition));
        }

        #region StatefulService
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new[]
            {
                new ServiceReplicaListener(
                    context => new WebListenerCommunicationListener(context, 
                        new string[] { "ServiceEndpoint" }, uri =>
                        new WebHostBuilder().UseWebListener()
                                           .ConfigureServices(services => {
                                               services.AddSingleton<IReliableStateManager>(this.StateManager);
                                               services.AddSingleton<IServicePartition>(this.Partition);
                                               services.AddSingleton<ServiceMonitor>(this.ServiceMonitor);
                                               services.AddSingleton<ServiceContext>(this.Context);
                                               services.AddSingleton<HttpClient>(this.HttpClient);
                                           })
                                           .UseStartup<Startup>()
                                           .UseUrls(uri)
                                           .Build()), name: "web")
            };
        }

        protected override Task RunAsync(CancellationToken cancellationToken)
        {
            this.ServiceMonitor.StartMonitoring(cancellationToken);
            Task.Run(() => this.TrimmingDataAndReportLoadAsync(cancellationToken));

            return base.RunAsync(cancellationToken);
        }
        #endregion StatefulService

        #region Data load reporting and trimming
        private async Task TrimmingDataAndReportLoadAsync(CancellationToken cancellationToken)
        {
            ReliableCollectionRetry retry = new ReliableCollectionRetry();
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
                try
                {
#if ImmutableCollection
                    var entities = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, ValuesEntity>>("Values");
                    await retry.RunAsync((Func<Task>)(async () =>
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
#else
                    var entities = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, string>>("Values");
                    await retry.RunAsync((Func<Task>)(async () =>
                    {
                        DateTimeOffset now = DateTimeOffset.UtcNow;
                        using (var tx = this.StateManager.CreateTransaction())
                        {
                            List<string> dataToRemove = new List<string>();
                            var values = await entities.CreateEnumerableAsync(tx);
                            using (var e = values.GetAsyncEnumerator())
                            {
                                while (await e.MoveNextAsync(cancellationToken))
                                {
                                    ValuesEntity entity = JsonConvert.DeserializeObject<ValuesEntity>(e.Current.Value);
                                    if (now.Subtract(entity.LastAccessedOn).TotalHours > 1)
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
#endif

                    long count;
                    using (var tx = this.StateManager.CreateTransaction())
                    {
                        count = await entities.GetCountAsync(tx);
                    }
                    // TODO: Report additional load  metrics of your application. The load metric needs to be included in 
                    // the ApplicationManifest.xml file.
                    this.Partition.ReportLoad(new LoadMetric[] { new LoadMetric("ValuesService.DataCount", (int)count) });
                }
                catch(Exception)
                { }
            }
        }
#endregion
        
#region Provide ServiceMonitor
        internal ServiceMonitor ServiceMonitor { get { return this.serviceMonitor.Value; } }
        private Lazy<ServiceMonitor> serviceMonitor;
#endregion

#region Provide HttpClient
        internal HttpClient HttpClient { get { return this.httpClient.Value; } }

        private HttpClient CreateHttpClient()
        {
            // TODO: 
            //  - One can further customize the Http client behavior by customizing the HttpClientHandler, or by adjusting 
            // ServicePointManager properties.
            return HttpClientFactory.Create(new HttpClientHandler(),
                                            new HttpTraceMessageHandler(this.Context)   // Adds correlation Id tracing to the HTTP request
                                            ); 
        }

        private Lazy<HttpClient> httpClient;
#endregion
    }
}
