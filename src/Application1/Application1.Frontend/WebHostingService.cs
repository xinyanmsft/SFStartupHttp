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
            this.serviceMonitor = new Lazy<ServiceMonitor>(() => new ServiceMonitor(this.Context, this.Partition));
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
                                                   services.AddSingleton<ServiceMonitor>(this.ServiceMonitor);
                                                   services.AddSingleton<ServiceContext>(this.Context);
                                                   services.AddSingleton<HttpClient>(this.HttpClient);
                                               })
                                           .Build()), name: "web")
            };
        }

        protected override Task RunAsync(CancellationToken cancellationToken)
        {
            this.ServiceMonitor.StartMonitoring(cancellationToken);

            return base.RunAsync(cancellationToken);
        }
        #endregion StatelessService

        #region Provide ServiceMonitor
        internal ServiceMonitor ServiceMonitor {  get { return this.serviceMonitor.Value; } }
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
