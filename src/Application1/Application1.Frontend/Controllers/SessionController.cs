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
        private HttpClient httpClient;
        private ServiceContext serviceContext;

        public SessionController(HttpClient httpClient, ServiceContext serviceContext)
        {
            this.httpClient = httpClient;
            this.serviceContext = serviceContext;
        }
        
        [HttpGet("{sessionId}")]
        public async Task<IActionResult> Get(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return this.BadRequest();
            }

            var partitionKey = this.GetSessionServicePartitionKey(sessionId);
            string requestUri = new NamedService(new NamedApplication(), "UserSessionService").BuildEndpointUri(endpointName: "web", target: HttpServiceUriTarget.Primary, partitionKey: partitionKey)
                                    + $"api/session/{sessionId}";
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage r = await this.httpClient.SendAsync(request);
            if (!r.IsSuccessStatusCode)
            {
                return new StatusCodeResult((int)r.StatusCode);
            }
            return new ContentResult()
            {
                Content = await r.Content.ReadAsStringAsync(),
                ContentType = "application/json",
                StatusCode = (int)r.StatusCode
            };
        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            string newSessionId = Guid.NewGuid().ToString();
            var partitionKey = this.GetSessionServicePartitionKey(newSessionId);
            string requestUri = new NamedService(new NamedApplication(), "UserSessionService").BuildEndpointUri(endpointName: "web", target: HttpServiceUriTarget.Primary, partitionKey: partitionKey)
                                    + $"api/session/{newSessionId}";
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = new StreamContent(this.HttpContext.Request.Body)
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage r = await this.httpClient.SendAsync(request);
            if (!r.IsSuccessStatusCode)
            {
                return new StatusCodeResult((int)r.StatusCode);
            }
            return new ContentResult()
            {
                Content = await r.Content.ReadAsStringAsync(),
                ContentType = "application/json",
                StatusCode = (int)r.StatusCode
            };
        }
        
        [HttpPut("{sessionId}")]
        public async Task<IActionResult> Put(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return this.BadRequest();
            }

            var partitionKey = this.GetSessionServicePartitionKey(sessionId);
            string requestUri = new NamedService(new NamedApplication(), "UserSessionService").BuildEndpointUri(endpointName: "web", target: HttpServiceUriTarget.Primary, partitionKey: partitionKey)
                                    + $"api/session/{sessionId}";
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, requestUri)
            {
                Content = new StreamContent(this.Request.Body)
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage r = await this.httpClient.SendAsync(request);
            if (!r.IsSuccessStatusCode)
            {
                return new StatusCodeResult((int)r.StatusCode);
            }

            return new ContentResult()
            {
                Content = await r.Content.ReadAsStringAsync(),
                ContentType = "application/json",
                StatusCode = (int)r.StatusCode
            };
        }
        
        [HttpDelete("{sessionId}")]
        public async Task<IActionResult> Delete(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return this.BadRequest();
            }

            var partitionKey = this.GetSessionServicePartitionKey(sessionId);
            string requestUri = new NamedService(new NamedApplication(), "UserSessionService").BuildEndpointUri(endpointName: "web", target: HttpServiceUriTarget.Primary, partitionKey: partitionKey)
                                    + $"api/session/{sessionId}";
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, requestUri);
            HttpResponseMessage r = await this.httpClient.SendAsync(request);

            return new StatusCodeResult((int)r.StatusCode);
        }

        private long GetSessionServicePartitionKey(string sessionId)
        {
            return 0;  /*TODO: comments*/
        }
    }
}
