using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ServiceFabric.Http.Client
{
    public sealed class HttpServiceClientExceptionHandler : DelegatingHandler
    {
        public HttpServiceClientExceptionHandler(HttpMessageHandler innerHandler) : base(innerHandler)
        { }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                return base.SendAsync(request, cancellationToken);
            }
            catch (TimeoutException ex)
            {
                throw new NeedsResolveServiceEndpointException("Timeout error", ex);
            }
            catch (SocketException ex)
            {
                throw new NeedsResolveServiceEndpointException("Socket error", ex);
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is WebException)
            {
                WebException we = ex as WebException;
                if (we == null)
                {
                    we = ex.InnerException as WebException;
                }
                if (we != null)
                {
                    // the following assumes port sharing
                    // where a port is shared by multiple replicas within a host process using a single web host (e.g., http.sys).
                    if (we.Status == WebExceptionStatus.Timeout ||
                        we.Status == WebExceptionStatus.RequestCanceled ||
                        we.Status == WebExceptionStatus.ConnectionClosed ||
                        we.Status == WebExceptionStatus.ConnectFailure)
                    {
                        throw new NeedsResolveServiceEndpointException(we.Status.ToString(), ex);
                    }
                }
                throw;
            }
        }
    }
}
