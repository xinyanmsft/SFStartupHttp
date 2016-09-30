using Application1.Frontend.Utility;
using Microsoft.AspNetCore.Mvc;
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
        public async Task<IActionResult> GetAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return this.BadRequest();
            }

            var partitionKey = ServiceUtility.GetValuesPartitionKey(id);
            Uri serviceUri = ServiceUtility.GetServiceUri(this.serviceContext, "ValuesService");
            Uri requestUri = ServiceUtility.BuildReverseProxyHttpRequestUri(serviceUri, path: $"api/values/{id}", partitionKey: partitionKey);

            return new ContentResult
            {
                StatusCode = 200,
                Content = await this.httpClient.GetStringAsync(requestUri),
                ContentType = "application/json"
            };
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync()
        {
            string newId = Guid.NewGuid().ToString();
            var partitionKey = ServiceUtility.GetValuesPartitionKey(newId);
            Uri serviceUri = ServiceUtility.GetServiceUri(this.serviceContext, "ValuesService");
            Uri requestUri = ServiceUtility.BuildReverseProxyHttpRequestUri(serviceUri, path: $"api/values/{newId}", partitionKey: partitionKey);

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

            var partitionKey = ServiceUtility.GetValuesPartitionKey(id);
            Uri serviceUri = ServiceUtility.GetServiceUri(this.serviceContext, "ValuesService");
            Uri requestUri = ServiceUtility.BuildReverseProxyHttpRequestUri(serviceUri, path: $"api/values/{id}", partitionKey: partitionKey);

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

            var partitionKey = ServiceUtility.GetValuesPartitionKey(id);
            Uri serviceUri = ServiceUtility.GetServiceUri(this.serviceContext, "ValuesService");
            Uri requestUri = ServiceUtility.BuildReverseProxyHttpRequestUri(serviceUri, path: $"api/values/{id}", partitionKey: partitionKey);

            HttpResponseMessage r = await this.httpClient.DeleteAsync(requestUri);
            return new StatusCodeResult((int)r.StatusCode);
        }        
    }
}
