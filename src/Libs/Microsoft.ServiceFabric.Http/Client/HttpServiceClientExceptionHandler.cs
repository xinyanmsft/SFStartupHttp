using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ServiceFabric.Http.Client
{
    /// <summary>
    /// HttpServiceClientExceptionHandler is a HTTP message handler. It needs to be wrapped by HttpServiceClientHandler. 
    /// It tells the HttpServiceClientHandler to re-resolve a HTTP endpoint from Service Fabric reliable service when certain 
    /// communication failures are met, such as TimeoutException, SocketException or connection being closed.
    /// </summary>
    public sealed class HttpServiceClientExceptionHandler : DelegatingHandler
    {
        public HttpServiceClientExceptionHandler(HttpMessageHandler innerHandler) : base(innerHandler)
        { }

        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                return await base.SendAsync(request, cancellationToken);
            }
            catch (TimeoutException ex)
            {
                throw new NeedsResolveServiceEndpointException("Timeout error", ex);
            }
            catch (SocketException ex)
            {
                throw new NeedsResolveServiceEndpointException("Socket error", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new NeedsResolveServiceEndpointException("Task cancelled", ex);
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
                        we.Status == WebExceptionStatus.NameResolutionFailure ||
                        we.Status == WebExceptionStatus.KeepAliveFailure ||
                        we.Status == WebExceptionStatus.ReceiveFailure ||
                        we.Status == WebExceptionStatus.SendFailure ||
                        we.Status == WebExceptionStatus.RequestCanceled ||
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
