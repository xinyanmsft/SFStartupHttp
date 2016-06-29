using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Http;
using Microsoft.ServiceFabric.Http.Client;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Application1.Frontend
{
    /// <summary>
    /// A specialized stateless service for hosting ASP.NET Core web apps.
    /// </summary>
    internal sealed class WebHostingService : StatelessService
    {
        public WebHostingService(StatelessServiceContext serviceContext)
            : base(serviceContext)
        {
            this.httpClient = new Lazy<HttpClient>(this.CreateHttpClient);
            this.serviceWatchdog = new Lazy<ServiceWatchdog>(() => new ServiceWatchdog(this.Context, this.Partition));
        }

        #region StatelessService
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new[]
            {
                new ServiceInstanceListener(context =>
                    new WebHostCommunicationListener(context, "ServiceEndpoint", uri =>
                        new WebHostBuilder().UseWebListener()
                                           .UseContentRoot(Directory.GetCurrentDirectory())
                                           .UseStartup<Startup>()
                                           .UseUrls(uri)
                                           .ConfigureServices(services =>
                                               {
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

            return base.RunAsync(cancellationToken);
        }
        #endregion StatelessService

        #region Provide ServiceWatchdog
        internal ServiceWatchdog ServiceWatchdog {  get { return this.serviceWatchdog.Value; } }
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
