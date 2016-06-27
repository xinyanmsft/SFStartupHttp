using Microsoft.AspNetCore.Http;
using System;
using System.Diagnostics;
using System.Fabric;
using System.Fabric.Health;
using System.Threading;

namespace Application1.Frontend
{
    internal sealed class ServiceWatchdog
    {
        public ServiceWatchdog(ServiceContext serviceContext, IServicePartition partition)
        {
            if (serviceContext == null)
            {
                throw new ArgumentNullException(nameof(serviceContext));
            }

            if (partition == null)
            {
                throw new ArgumentNullException(nameof(partition));
            }

            this.serviceContext = serviceContext;
            this.partition = partition;
        }

        public void StartMonitoring(CancellationToken cancellationToken)
        {
            this.StartMonitoring(cancellationToken, DefaultLoadReportFrequency, DefaultResponseTimeWarningThreshold);
        }

        public void StartMonitoring(CancellationToken cancellationToken, TimeSpan loadReportFrequency, TimeSpan responseTimeWarningThreshold)
        {
            this.totalRequests = 0;
            this.concurrentRequests = 0;
            this.responseTimeWarningThreshold = responseTimeWarningThreshold;
            this.isMonitoring = true;

            Stopwatch stopwatch = new Stopwatch();
            this.loadReportTimer = new Timer((state) =>
            {
                stopwatch.Stop();
                if (!cancellationToken.IsCancellationRequested)
                {
                    var totalRequests = Interlocked.Exchange(ref this.totalRequests, 0);
                    int concurrentRequests = (int)Interlocked.Read(ref this.concurrentRequests);
                    double reportIntervalSeconds = stopwatch.Elapsed.TotalSeconds;

                    stopwatch.Restart();
                    int requestsPerSecond = Convert.ToInt32((double)totalRequests / reportIntervalSeconds);
                    this.partition.ReportLoad(new LoadMetric[] { new LoadMetric("Frontend.RequestPerSecond", requestsPerSecond),
                                                                 new LoadMetric("Frontend.ConcurrentRequest", concurrentRequests) });

                    this.loadReportTimer.Change(loadReportFrequency, Timeout.InfiniteTimeSpan);
                }
                else
                {
                    this.isMonitoring = false;
                }
            }, null, loadReportFrequency, Timeout.InfiniteTimeSpan);
            stopwatch.Start();
        }

        public void OnRequestStart(HttpContext httpContext)
        {
            ServiceEventSource.Current.ServiceRequestStart(this.serviceContext, httpContext.Request.Method,  httpContext.Request.Path);

            // TODO: Capture load metrics relevant to your application
            Interlocked.Increment(ref this.totalRequests);
            Interlocked.Increment(ref this.concurrentRequests);
        }

        public void OnRequestException(HttpContext httpContext, Exception exception)
        {
            ServiceEventSource.Current.ServiceRequestFailed(this.serviceContext, httpContext.Request.Method, httpContext.Request.Path, exception.ToString());
        }

        public void OnRequestStop(HttpContext httpContext, TimeSpan duration)
        {
            // TODO: Method, StatusCode
            ServiceEventSource.Current.ServiceRequestStop(this.serviceContext, httpContext.Request.Method, httpContext.Request.Path, httpContext.Response.StatusCode);

            // TODO: Capture load metrics relevant to your application
            Interlocked.Decrement(ref this.concurrentRequests);

            if (this.isMonitoring && duration > this.responseTimeWarningThreshold)
            {
                var healthInfo = new HealthInformation(this.serviceContext.ServiceName.OriginalString, "ResponseTime", HealthState.Warning)
                {
                    TimeToLive = DefaultHealthEventTimeToLive,
                    RemoveWhenExpired = true,
                    Description = $"It took {duration.TotalSeconds} seconds to process request {httpContext.Request.Path}"
                };
            }
        }

        #region private members
        private readonly ServiceContext serviceContext;
        private readonly IServicePartition partition;

        private static readonly TimeSpan DefaultLoadReportFrequency = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan DefaultResponseTimeWarningThreshold = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan DefaultHealthEventTimeToLive = TimeSpan.FromSeconds(120);

        private long concurrentRequests = 0;
        private long totalRequests = 0;
        private Timer loadReportTimer;
        private TimeSpan responseTimeWarningThreshold;
        private volatile bool isMonitoring = false;
        #endregion
    }
}
