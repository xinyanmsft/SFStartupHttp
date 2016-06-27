using System;
using System.Net.Http;

namespace Microsoft.ServiceFabric.Http.Client
{
    public sealed class NeedsResolveServiceEndpointException : Exception
    {
        public NeedsResolveServiceEndpointException(string message, Exception inner) : base(message, inner)
        { }

        public NeedsResolveServiceEndpointException(string message, HttpResponseMessage response) : base(message)
        {
            this.Response = response;
        }

        public HttpResponseMessage Response { get; private set; }
    }
}
