using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Http.Client;
using System;
using System.Fabric;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Application1.Frontend.Controllers
{
    [Route("api/[controller]")]
    public sealed class SessionController : Controller
    {
        private readonly HttpClient httpClient;
        private readonly ServiceContext serviceContext;

        public SessionController(HttpClient httpClient, ServiceContext serviceContext)
        {
            this.httpClient = httpClient;
            this.serviceContext = serviceContext;
        }
        
        [HttpGet("{sessionId}")]
        public async Task<IActionResult> GetAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return this.BadRequest();
            }

            var partitionKey = this.GetSessionServicePartitionKey(sessionId);
            string requestUri = new NamedApplication(this.serviceContext)
                                    .AppendNamedService("UserSessionService")
                                    .BuildEndpointUri(endpointName: "web", target: HttpServiceUriTarget.Primary, partitionKey: partitionKey)
                                    + $"api/sessiondata/{sessionId}";
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
            string newSessionId = Guid.NewGuid().ToString();
            var partitionKey = this.GetSessionServicePartitionKey(newSessionId);
            string requestUri = new NamedApplication(this.serviceContext)
                                    .AppendNamedService("UserSessionService")
                                    .BuildEndpointUri(endpointName: "web", target: HttpServiceUriTarget.Primary, partitionKey: partitionKey)
                                    + $"api/sessiondata/{newSessionId}";    // TODO: feedback: do not do string append. 
            HttpResponseMessage r = await this.httpClient.PostAsync(requestUri, new StreamContent(this.HttpContext.Request.Body));
            r.EnsureSuccessStatusCode();
            return new ContentResult
            {
                Content = await r.Content.ReadAsStringAsync(),
                ContentType = "application/json",
                StatusCode = (int)r.StatusCode
            };
        }
        
        [HttpPut("{sessionId}")]
        public async Task<IActionResult> PutAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return this.BadRequest();
            }

            var partitionKey = this.GetSessionServicePartitionKey(sessionId);
            string requestUri = new NamedApplication(this.serviceContext)
                                    .AppendNamedService("UserSessionService")
                                    .BuildEndpointUri(endpointName: "web", target: HttpServiceUriTarget.Primary, partitionKey: partitionKey)
                                    + $"api/sessiondata/{sessionId}";
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
        
        [HttpDelete("{sessionId}")]
        public async Task<IActionResult> DeleteAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return this.BadRequest();
            }

            var partitionKey = this.GetSessionServicePartitionKey(sessionId);
            string requestUri = new NamedApplication(this.serviceContext)
                                    .AppendNamedService("UserSessionService")
                                    .BuildEndpointUri(endpointName: "web", target: HttpServiceUriTarget.Primary, partitionKey: partitionKey)
                                    + $"api/sessiondata/{sessionId}";
            HttpResponseMessage r = await this.httpClient.DeleteAsync(requestUri);
            return new StatusCodeResult((int)r.StatusCode);
        }

        private long GetSessionServicePartitionKey(string sessionId)
        {
            return 0;  /*TODO: comments*/
        }
    }
}
