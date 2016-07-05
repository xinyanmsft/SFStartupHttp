using Microsoft.ServiceFabric.Services.Client;
using System;

namespace Microsoft.ServiceFabric.Http.Client
{
    public static class NamedServiceExtensions
    {
        public static string BuildEndpointUri(this NamedService service, 
                                              string endpointName,
                                              EndpointScheme scheme = EndpointScheme.HTTP)
        {
            return BuildEndpointUri(service, endpointName, HttpServiceUriTarget.Any, null, scheme.ToString());
        }

        public static string BuildEndpointUri(this NamedService service,
                                              string endpointName,
                                              HttpServiceUriTarget target,
                                              long partitionKey,
                                              EndpointScheme scheme = EndpointScheme.HTTP)
        {
            return BuildEndpointUri(service, endpointName, target, new ServicePartitionKey(partitionKey), scheme.ToString());
        }

        private static string BuildEndpointUri(NamedService service, string endpointName, HttpServiceUriTarget target, ServicePartitionKey partitionKey, string scheme)
        { 
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            if (string.IsNullOrEmpty(scheme))
            {
                throw new ArgumentException(nameof(scheme));
            }

            HttpServiceUriBuilder builder = new HttpServiceUriBuilder();
            builder.SetScheme(scheme);
            builder.SetServiceName(service);
            builder.SetPartitionKey(partitionKey);
            builder.SetEndpointName(endpointName);
            builder.SetTarget(target);

            return builder.Build().OriginalString;
        }
    }
}
