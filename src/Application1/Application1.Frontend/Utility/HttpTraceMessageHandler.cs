using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Application1.Frontend.Utility
{
    /// <summary>
    /// HttpTraceMessageHandler is a HTTP message handler that traces the request by 1) flows the CorrelationId 
    /// across service boundary and 2) output the traces to ETW.
    /// </summary>
    internal class HttpTraceMessageHandler : DelegatingHandler
    {
        public HttpTraceMessageHandler(ServiceContext context) : base()
        {
            this.context = context;
        }

        public HttpTraceMessageHandler(HttpMessageHandler innerHandler, ServiceContext context) : base(innerHandler)
        {
            this.context = context;
        }
        
        #region DelegatingHandler override
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!request.Headers.Contains(HttpCorrelation.CorrelationHeaderName))
            {
                SetCorrelationHeader(request, HttpCorrelation.GetRequestCorrelationId());
            }

            if (!request.Headers.Contains(HttpCorrelation.RequestOriginHeaderName) && this.context != null)
            {
                SetRequestOriginHeader(request, this.context.ServiceName.ToString());
            }

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            string origin = GetHeaderValue(request, HttpCorrelation.RequestOriginHeaderName);
            string correlationId = GetHeaderValue(request, HttpCorrelation.CorrelationHeaderName);

            ServiceEventSource.Current.HttpClientRequestStart(request, origin, correlationId);
            try
            {
                return await base.SendAsync(request, cancellationToken);
            }
            catch(Exception ex)
            {
                ServiceEventSource.Current.HttpClientRequestFailed(request, origin, correlationId, ex);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                ServiceEventSource.Current.HttpClientRequestStop(request, origin, correlationId, stopwatch.Elapsed.TotalMilliseconds);
            }
        }
        #endregion

        #region public static members
        public static void SetCorrelationHeader(HttpRequestMessage request, string correlationId)
        {
            if (!string.IsNullOrEmpty(correlationId))
            {
                request.Headers.TryAddWithoutValidation(HttpCorrelation.CorrelationHeaderName, correlationId);
            }
        }

        public static void SetRequestOriginHeader(HttpRequestMessage request, string serviceName)
        {
            if (!string.IsNullOrEmpty(serviceName))
            {
                request.Headers.TryAddWithoutValidation(HttpCorrelation.RequestOriginHeaderName, serviceName);
            }
        }
        #endregion

        #region private members
        private static string GetHeaderValue(HttpRequestMessage request, string headerName)
        {
            IEnumerable<string> values;
            return request.Headers.TryGetValues(headerName, out values) ? values.FirstOrDefault() : string.Empty;
        }

        private readonly ServiceContext context;
        #endregion
    }
}

