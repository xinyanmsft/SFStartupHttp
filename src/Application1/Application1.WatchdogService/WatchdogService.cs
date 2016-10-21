
using Application1.WatchdogService.Utility;
using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Fabric.Health;
using System.Net.Http;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace Application1.WatchdogService
{
    /// <summary>
    /// The WatchdogService is responsible for continuously monitor other services, and report Service Fabric health events.
    /// </summary>
    internal sealed class WatchdogService : StatelessService
    {
        public WatchdogService(StatelessServiceContext context)
            : base(context)
        {
            this.fabricClient = new FabricClient();
        }
        
        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            string applicationName = FabricRuntime.GetActivationContext().ApplicationName;
            System.Threading.Thread.Sleep(1000000);
            while (true)
            {
                await Task.Delay(this.healthCheckFrequency, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                HttpClient httpClient = this.CreateHttpClient();
                await Task.WhenAll(
                   // TODO: Add additional health checks for your application
                   this.CheckServiceHealthAsync(new Uri($"{applicationName}/Frontend"), async (serviceName, partition) =>
                   {
                       Uri requestUri = ServiceUtility.BuildReverseProxyHttpRequestUri(serviceName, path: "default.html", timeoutInSeconds: 60);
                       var response = await httpClient.GetAsync(requestUri);
                       return response.IsSuccessStatusCode ? null : $"Request {response.RequestMessage.RequestUri} failed with {response.StatusCode}";
                   }, cancellationToken),
                   this.CheckServiceHealthAsync(new Uri($"{applicationName}/ValuesService"), async (serviceName, partition) =>
                   {
                       Uri requestUri = ServiceUtility.BuildReverseProxyHttpRequestUri(serviceName, path: "api/values/", partitionKey: new ServicePartitionKey(0), timeoutInSeconds: 60);
                       var response = await httpClient.GetAsync(requestUri);
                       return response.IsSuccessStatusCode ? null : $"Request {response.RequestMessage.RequestUri} failed with {response.StatusCode}";
                   }, cancellationToken));

                this.Partition.ReportPartitionHealth(this.RunSelfCheck());
            }
        }

        #region private members
        private HealthInformation RunSelfCheck()
        {
            WindowsPrincipal p = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            bool isAdmin = p.IsInRole(WindowsBuiltInRole.Administrator);
            if (!isAdmin)
            {
                // Report health through FabricClient requires admin.
                return new HealthInformation(this.Context.ServiceName.ToString(), "SelfMonitoring", HealthState.Warning)
                {
                    TimeToLive = this.healthCheckFrequency.Add(this.healthInfoTimeToLive),
                    Description = "Watchdog service requires administrative privilege.",
                    RemoveWhenExpired = false
                };
            }
            return new HealthInformation(this.Context.ServiceName.ToString(), "SelfMonitoring", HealthState.Ok)
            {
                TimeToLive = this.healthCheckFrequency.Add(this.healthInfoTimeToLive),
                Description = "Watchdog self monitoring",
                RemoveWhenExpired = true
            };
        }

        private async Task CheckServiceHealthAsync(Uri serviceName, Func<Uri, ServicePartitionInformation, Task<string>> func, CancellationToken cancellationToken)
        {
            HttpCorrelation.SetRequestCorrelationId(Guid.NewGuid().ToString());
            var partitionList = await fabricClient.QueryManager.GetPartitionListAsync(serviceName);
            foreach (var partition in partitionList)
            {
                HealthInformation healthInfo = null;
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                try
                {
                    string message = await func(serviceName, partition.PartitionInformation);
                    if (!string.IsNullOrEmpty(message))
                    {
                        healthInfo = this.ToHealthInformation(HealthCheckPropertyName, 
                                                              HealthState.Warning,
                                                              this.healthInfoTimeToLive,
                                                              $"Health check of {serviceName}, partition {partition.PartitionInformation.Id} failed with {message}", 
                                                              removedWhenExpired: true);
                    }
                }
                catch (Exception ex)
                {
                    healthInfo = this.ToHealthInformation(HealthCheckPropertyName, 
                                                          HealthState.Warning, 
                                                          this.healthInfoTimeToLive,
                                                          $"Health check of {serviceName}, partition {partition.PartitionInformation.Id} failed with exception {ex}", 
                                                          removedWhenExpired: true);
                }
                finally
                {
                    stopwatch.Stop();
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        if (healthInfo == null)
                        {
                            if (stopwatch.Elapsed > responseTimeWarningThreshold)
                            {
                                healthInfo = this.ToHealthInformation(HealthCheckPropertyName, 
                                                                      HealthState.Warning, 
                                                                      this.healthInfoTimeToLive,
                                                                      $"Health check of {serviceName}, partition {partition.PartitionInformation.Id} took {stopwatch.Elapsed.TotalSeconds} seconds to complete.",
                                                                      removedWhenExpired: true);
                            }
                            else
                            {
                                healthInfo = this.ToHealthInformation(HealthCheckPropertyName, 
                                                                      HealthState.Ok, 
                                                                      this.healthInfoTimeToLive, 
                                                                      "OK", 
                                                                      removedWhenExpired: true);
                            }
                        }

                        this.fabricClient.HealthManager.ReportHealth(new PartitionHealthReport(partition.PartitionInformation.Id, healthInfo));
                    }
                }
            }
        }

        private HttpClient CreateHttpClient()
        {
            // TODO: 
            //  - To enable circuit breaker pattern, set proper values in CircuitBreakerHttpMessageHandler constructor.
            //  - To change when to re-resolve Service Fabric reliable service address, customize or replace 
            // HttpServiceClientExceptionHandler and HttpServiceClientStatusCodeRetryHandler.
            //  - One can further customize the Http client behavior by customizing the HttpClientHandler, or by adjusting 
            // ServicePointManager properties.
            return HttpClientFactory.Create(new HttpClientHandler(),
                                            new HttpTraceMessageHandler(this.Context)   // Adds correlation Id tracing to the HTTP request
                                            );
        }

        private HealthInformation ToHealthInformation(string property, HealthState state, TimeSpan ttl, String description, Boolean removedWhenExpired = false, Int64 sequenceNumber = HealthInformation.UnknownSequenceNumber)
        {
            return new HealthInformation(this.Context.ServiceName.ToString(), property, state)
            {
                TimeToLive = ttl,
                RemoveWhenExpired = removedWhenExpired,
                Description = description,
                SequenceNumber = sequenceNumber
            };
        }
        
        private readonly FabricClient fabricClient;
        private readonly TimeSpan healthCheckFrequency = TimeSpan.FromSeconds(60);
        private readonly TimeSpan healthInfoTimeToLive = TimeSpan.FromSeconds(300);
        private readonly TimeSpan responseTimeWarningThreshold = TimeSpan.FromSeconds(30);
        private const string HealthCheckPropertyName = "Watchdog.HealthCheck";
        #endregion
    }
}
