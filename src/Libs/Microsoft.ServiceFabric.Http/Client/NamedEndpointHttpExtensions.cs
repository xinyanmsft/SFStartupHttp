using Microsoft.ServiceFabric.Services.Client;
using System;

namespace Microsoft.ServiceFabric.Http.Client
{
    public static class NamedEndpointHttpExtensions
    {
        public static string BuildHttpUri(this NamedEndpoint endpoint, 
                                          string path = null,
                                          EndpointScheme scheme = EndpointScheme.HTTP)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }
            
            HttpServiceUriBuilder builder = new HttpServiceUriBuilder();
            builder.SetScheme(scheme.ToString());
            builder.SetServiceName(endpoint.Service);
            builder.SetPartitionKey(endpoint.PartitionKey);
            builder.SetEndpointName(endpoint.EndpointName);
            builder.SetTarget(endpoint.Target);

            if (string.IsNullOrEmpty(path))
            {
                return builder.Build().OriginalString;
            }
            else
            {
                return builder.Build().OriginalString + path;
            }
        }
    }
}
