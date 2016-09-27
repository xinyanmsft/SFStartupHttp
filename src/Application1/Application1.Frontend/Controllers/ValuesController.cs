using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Http.Client;
using Newtonsoft.Json;
using System;
using System.Fabric;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Application1.Frontend.Controllers
{
    [Route("api/[controller]")]
    public sealed class ValuesController : Controller
    {
        private readonly HttpClient httpClient;
        private readonly ServiceContext serviceContext;

        public ValuesController(HttpClient httpClient, ServiceContext serviceContext)
        {
            this.httpClient = httpClient;
            this.serviceContext = serviceContext;
        }
        
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAsync(string id, int numThread, bool useReverseProxy = false)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return this.BadRequest();
            }

            if (StringComparer.OrdinalIgnoreCase.Equals(id, "start1"))
            {
                PerfTests test = new PerfTests(this.httpClient, this.serviceContext, useReverseProxy);
                TestResult testResult = test.Test(1, numThread);
                return new ContentResult
                {
                    StatusCode = 200,
                    Content = JsonConvert.SerializeObject(testResult),
                    ContentType = "application/json"
                };
            }
            else if (StringComparer.OrdinalIgnoreCase.Equals(id, "start2"))
            {
                PerfTests test = new PerfTests(this.httpClient, this.serviceContext, useReverseProxy);
                TestResult testResult = test.Test(2, numThread);
                return new ContentResult
                {
                    StatusCode = 200,
                    Content = JsonConvert.SerializeObject(testResult),
                    ContentType = "application/json"
                };
            }
            else
            {
                var partitionKey = this.GetValuesPartitionKey(id);
                string requestUri = new NamedApplication(this.serviceContext)
                                        .AppendNamedService("ValuesService")
                                        .AppendNamedEndpoint(endpointName: "web", target: ServiceTarget.Primary, partitionKey: partitionKey)
                                        .BuildHttpUri($"api/values/{id}");
                return new ContentResult
                {
                    StatusCode = 200,
                    Content = await this.httpClient.GetStringAsync(requestUri),
                    ContentType = "application/json"
                };
            }
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync()
        {
            string newId = Guid.NewGuid().ToString();
            var partitionKey = this.GetValuesPartitionKey(newId);
            string requestUri = new NamedApplication(this.serviceContext)
                                    .AppendNamedService("ValuesService")
                                    .AppendNamedEndpoint(endpointName: "web", target: ServiceTarget.Primary, partitionKey: partitionKey)
                                    .BuildHttpUri($"api/values/{newId}"); 
            HttpResponseMessage r = await this.httpClient.PostAsync(requestUri, new StreamContent(this.HttpContext.Request.Body));
            r.EnsureSuccessStatusCode();
            return new ContentResult
            {
                Content = await r.Content.ReadAsStringAsync(),
                ContentType = "application/json",
                StatusCode = (int)r.StatusCode
            };
        }
        
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return this.BadRequest();
            }

            var partitionKey = this.GetValuesPartitionKey(id);
            string requestUri = new NamedApplication(this.serviceContext)
                                    .AppendNamedService("ValuesService")
                                    .AppendNamedEndpoint(endpointName: "web", target: ServiceTarget.Primary, partitionKey: partitionKey)
                                    .BuildHttpUri($"api/values/{id}");
            HttpContent content = new StreamContent(this.Request.Body);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            HttpResponseMessage r = await this.httpClient.PutAsync(requestUri, content);
            r.EnsureSuccessStatusCode();
            return new ContentResult
            {
                Content = await r.Content.ReadAsStringAsync(),
                ContentType = "application/json",
                StatusCode = (int)r.StatusCode
            };
        }
        
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return this.BadRequest();
            }

            var partitionKey = this.GetValuesPartitionKey(id);
            string requestUri = new NamedApplication(this.serviceContext)
                                    .AppendNamedService("ValuesService")
                                    .AppendNamedEndpoint(endpointName: "web", target: ServiceTarget.Primary, partitionKey: partitionKey)
                                    .BuildHttpUri($"api/values/{id}");
            HttpResponseMessage r = await this.httpClient.DeleteAsync(requestUri);
            return new StatusCodeResult((int)r.StatusCode);
        }

        private long GetValuesPartitionKey(string id)
        {
            // When working with Service Fabric stateful service and reliable collection, one needs to understand
            // how the Service Fabric partition works, and come up with a good partition strategy for the application.
            // Please read these articles and change this method to return the partition key. 
            // https://azure.microsoft.com/en-us/documentation/articles/service-fabric-concepts-partitioning/
            // https://azure.microsoft.com/en-us/documentation/articles/service-fabric-reliable-services-reliable-collections/
            return id == null ? 0 : ((long)id.GetHashCode()) << 32; 
        }
    }
}
