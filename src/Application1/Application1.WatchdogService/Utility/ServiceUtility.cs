using Microsoft.ServiceFabric.Services.Client;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace Application1.WatchdogService.Utility
{
    internal static class ServiceUtility
    {
        public static ServicePartitionKey GetValuesPartitionKey(string id)
        {
            // When working with Service Fabric stateful service and reliable collection, one needs to understand
            // how the Service Fabric partition works, and come up with a good partition strategy for the application.
            // Please read these articles and change this method to return the partition key. 
            // https://azure.microsoft.com/en-us/documentation/articles/service-fabric-concepts-partitioning/
            // https://azure.microsoft.com/en-us/documentation/articles/service-fabric-reliable-services-reliable-collections/

            return new ServicePartitionKey(0);
        }

        public static Uri GetServiceUri(ServiceContext serviceContext, string serviceName)
        {
            return new Uri($"{serviceContext.CodePackageActivationContext.ApplicationName}/{serviceName}");
        }

        public static Uri GetServiceUri(Uri applicationUri, string serviceName)
        {
            if (applicationUri.Scheme != "fabric")
            {
                throw new ArgumentException("Application uri must start with fabric:/");
            }
            return new Uri($"{applicationUri}/{serviceName}");
        }

        /// <summary>
        /// TODO: Support individual listeners, support Primary vs Secondary vs Any
        /// </summary>
        public static Uri BuildReverseProxyHttpRequestUri(Uri serviceUri,
                                                          string path = "",
                                                          ServicePartitionKey partitionKey = null,
                                                          double timeoutInSeconds = 10,
                                                          string scheme = "http",
                                                          string reverseProxyHost = "localhost",
                                                          int reverseProxyPort = 19081)
        {
            if (!StringComparer.OrdinalIgnoreCase.Equals(serviceUri.Scheme, "fabric"))
            {
                throw new ArgumentException(nameof(serviceUri));
            }

            Dictionary<string, string> reverseProxyQueryString = new Dictionary<string, string>();
            reverseProxyQueryString.Add(QueryString_Timeout, timeoutInSeconds.ToString());
            if (partitionKey != null)
            {
                reverseProxyQueryString.Add(QueryString_PartitionKind, partitionKey.Kind.ToString());
                reverseProxyQueryString.Add(QueryString_PartitionKey, partitionKey.Value == null ? "" : partitionKey.Value.ToString());
            }

            UriBuilder builder = new UriBuilder($"{scheme}://{reverseProxyHost}:{reverseProxyPort}{serviceUri.AbsolutePath}/{path}");
            var queryStrings = HttpUtility.ParseQueryString(builder.Query);
            if (queryStrings[QueryString_Timeout] != null ||
                queryStrings[QueryString_PartitionKey] != null ||
                queryStrings[QueryString_PartitionKind] != null)
            {
                throw new ArgumentException("Query string PartitionKey, PartitionKind and Timeout are reserved for reverse proxy use only.");
            }

            foreach (var s in reverseProxyQueryString)
            {
                queryStrings.Set(s.Key, s.Value);
            }
            builder.Query = queryStrings.ToString();
            
            return builder.Uri;
        }

        #region private members

        private const string QueryString_Timeout = "Timeout";
        private const string QueryString_PartitionKey = "PartitionKey";
        private const string QueryString_PartitionKind = "PartitionKind";
        #endregion
    }
}
