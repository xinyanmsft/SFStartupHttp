using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ServiceFabric.Http.Client
{
    public sealed class HttpTraceMessageHandler : DelegatingHandler
    {
        public HttpTraceMessageHandler(HttpMessageHandler innerHandler) : this(null, innerHandler)
        { }

        public HttpTraceMessageHandler(ServiceContext context, HttpMessageHandler innerHandler) : base(innerHandler)
        {
            this.context = context;
        }

        #region DelegatingHandler override
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!request.Headers.Contains(ServiceFabricDiagnostics.CorrelationHeaderName))
            {
                SetCorrelationHeader(request, ServiceFabricDiagnostics.GetRequestCorrelationId());
            }

            if (!request.Headers.Contains(ServiceFabricDiagnostics.RequestOriginHeaderName) && this.context != null)
            {
                SetRequestOriginHeader(request, this.context.ServiceName.ToString());
            }

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            string origin = GetHeaderValue(request, ServiceFabricDiagnostics.RequestOriginHeaderName);
            string correlationId = GetHeaderValue(request, ServiceFabricDiagnostics.CorrelationHeaderName);

            HttpClientEventSource.Current.HttpRequestStart(request, origin, correlationId);
            try
            {
                return base.SendAsync(request, cancellationToken);
            }
            catch(Exception ex)
            {
                HttpClientEventSource.Current.HttpRequestFailed(request, origin, correlationId, ex);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                HttpClientEventSource.Current.HttpRequestStop(request, origin, correlationId, stopwatch.Elapsed.TotalMilliseconds);
            }
        }
        #endregion

        #region public static members
        public static void SetCorrelationHeader(HttpRequestMessage request, string correlationId)
        {
            if (!string.IsNullOrEmpty(correlationId))
            {
                request.Headers.TryAddWithoutValidation(ServiceFabricDiagnostics.CorrelationHeaderName, correlationId);
            }
        }

        public static void SetRequestOriginHeader(HttpRequestMessage request, string serviceName)
        {
            if (!string.IsNullOrEmpty(serviceName))
            {
                request.Headers.TryAddWithoutValidation(ServiceFabricDiagnostics.RequestOriginHeaderName, serviceName);
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

