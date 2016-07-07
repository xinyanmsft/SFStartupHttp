using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ServiceFabric.Http.Client
{
    /// <summary>
    /// HttpServiceClientStatusCodeRetryHandler is a HTTP message handler. It needs to be wrapped by HttpServiceClientHandler. 
    /// It tells the HttpServiceClientHandler to re-resolve a HTTP endpoint from Service Fabric reliable service when certain 
    /// conditions are met, such as receiving 503 status code.
    /// </summary>
    public sealed class HttpServiceClientStatusCodeRetryHandler : DelegatingHandler
    {
        public HttpServiceClientStatusCodeRetryHandler(HttpMessageHandler innerHandler, int[] statusCodeToRetry = null) : base(innerHandler)
        {
            if (statusCodeToRetry == null)
            {
                this.statusCodeToRetry = new int[] { 503 };
            }
            else
            {
                this.statusCodeToRetry = statusCodeToRetry;
            }
        }

        #region delegating handler override
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                HttpResponseMessage response = await base.SendAsync(request, cancellationToken);
                if (this.ShouldResolveServiceEndpoint(response.StatusCode))
                {
                    throw new NeedsResolveServiceEndpointException("Status code error", response);
                }
                return response;
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is WebException)
            {
                WebException we = ex as WebException;
                if (we == null)
                {
                    we = ex.InnerException as WebException;
                }
                if (we != null && we.Status == WebExceptionStatus.ProtocolError)
                {
                    HttpWebResponse errorResponse = we.Response as HttpWebResponse;
                    if (errorResponse != null && this.ShouldResolveServiceEndpoint(errorResponse.StatusCode))
                    {
                        throw new NeedsResolveServiceEndpointException("Status code error", ex);
                    }
                }

                throw;
            }
        }
        #endregion

        #region private members
        private bool ShouldResolveServiceEndpoint(HttpStatusCode statusCode)
        {
            return this.statusCodeToRetry.Any(c => c == (int)statusCode);
        }

        private readonly int[] statusCodeToRetry;
        #endregion
    }
}
