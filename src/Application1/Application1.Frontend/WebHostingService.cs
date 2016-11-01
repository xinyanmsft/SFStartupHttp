using Application1.Frontend.Utility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Diagnostics.Correlation.Common.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
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
                new ServiceInstanceListener(
                    context => new WebListenerCommunicationListener(context, 
                        new string[] { "ServiceEndpoint" }, uri =>
                        new WebHostBuilder().UseWebListener()
                                           .ConfigureServices(services => {
                                               services.AddSingleton<IServicePartition>(this.Partition);
                                               services.AddSingleton<ServiceMonitor>(this.ServiceMonitor);
                                               services.AddSingleton<ServiceContext>(this.Context);
                                               services.AddSingleton<HttpClient>(this.HttpClient);
                                           })
                                           .UseContentRoot(Directory.GetCurrentDirectory())
                                           .UseStartup<Startup>()
                                           .UseUrls(uri)
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
            HttpClient client = CorrelationHttpClientBuilder.CreateClient();
            return client;
        }

        private Lazy<HttpClient> httpClient;
        #endregion
    }
}
