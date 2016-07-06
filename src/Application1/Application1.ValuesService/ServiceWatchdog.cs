using Microsoft.AspNetCore.Http;
using System;
using System.Fabric;
using System.Fabric.Health;
using System.Threading;

namespace Application1.ValuesService
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
            this.StartMonitoring(cancellationToken, DefaultResponseTimeWarningThreshold);
        }

        public void StartMonitoring(CancellationToken cancellationToken, TimeSpan responseTimeWarningThreshold)
        {
            this.responseTimeWarningThreshold = responseTimeWarningThreshold;
            this.cancellationToken = cancellationToken;
        }

        public void OnRequestStart(HttpContext httpContext)
        {
            ServiceEventSource.Current.ServiceRequestStart(this.serviceContext, httpContext.Request.Method,  httpContext.Request.Path);
        }

        public void OnRequestException(HttpContext httpContext, Exception exception)
        {
            ServiceEventSource.Current.ServiceRequestFailed(this.serviceContext, httpContext.Request.Method, httpContext.Request.Path, exception.ToString());
        }

        public void OnRequestStop(HttpContext httpContext, TimeSpan duration)
        {
            // TODO: Method, StatusCode
            ServiceEventSource.Current.ServiceRequestStop(this.serviceContext, httpContext.Request.Method, httpContext.Request.Path, httpContext.Response.StatusCode);

            if (!this.cancellationToken.IsCancellationRequested && duration > this.responseTimeWarningThreshold)
            {
                this.partition.ReportPartitionHealth(new HealthInformation(this.serviceContext.ServiceName.OriginalString, "ResponseTime", HealthState.Warning)
                {
                    TimeToLive = DefaultHealthEventTimeToLive,
                    RemoveWhenExpired = true,
                    Description = $"It took {duration.TotalSeconds} seconds to process request {httpContext.Request.Path}"
                });
            }
        }

        #region private members
        private readonly ServiceContext serviceContext;
        private readonly IServicePartition partition;

        private static readonly TimeSpan DefaultResponseTimeWarningThreshold = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan DefaultHealthEventTimeToLive = TimeSpan.FromSeconds(120);

        private TimeSpan responseTimeWarningThreshold;
        private CancellationToken cancellationToken;
        #endregion
    }
}
