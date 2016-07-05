using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ServiceFabric.Unittest.HttpClient
{
    public sealed class MockHttpClientHandler : DelegatingHandler
    {
        public Func<HttpRequestMessage, HttpResponseMessage> Handler { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Func<HttpRequestMessage, HttpResponseMessage> handler = this.Handler;
            return handler != null ? Task.FromResult(handler(request))
                                   : Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
