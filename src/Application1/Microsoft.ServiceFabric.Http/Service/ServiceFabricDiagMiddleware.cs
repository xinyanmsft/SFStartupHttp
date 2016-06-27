using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.ServiceFabric.Http.Service
{
    public sealed class ServiceFabricDiagMiddleware
    {
        public ServiceFabricDiagMiddleware(RequestDelegate next)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            _next = next;
        }

        public Task Invoke(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            this.FlowCorrelationIdAndRequestOrigin(context);
            return _next.Invoke(context);
        }

        #region private members
        private void FlowCorrelationIdAndRequestOrigin(HttpContext context)
        {
            string correlationId = null;
            StringValues correlationIdHeader, originHeader;
            if (context.Request.Headers.TryGetValue(ServiceFabricDiagnostics.CorrelationHeaderName, out correlationIdHeader))
            {
                correlationId = correlationIdHeader.FirstOrDefault();
            }
            if (String.IsNullOrWhiteSpace(correlationId))
            {
                correlationId = Guid.NewGuid().ToString("N");
            }

            if (context.Request.Headers.TryGetValue(ServiceFabricDiagnostics.RequestOriginHeaderName, out originHeader))
            {
                ServiceFabricDiagnostics.SetRequestOrigin(originHeader.FirstOrDefault());
            }

            ServiceFabricDiagnostics.SetRequestCorrelationId(correlationId);
        }

        private readonly RequestDelegate _next;
        #endregion
    }
}
