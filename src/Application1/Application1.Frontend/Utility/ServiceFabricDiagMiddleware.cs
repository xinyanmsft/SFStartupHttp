using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Application1.Frontend.Utility
{
    internal class ServiceFabricDiagMiddleware
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
            if (context.Request.Headers.TryGetValue(HttpCorrelation.CorrelationHeaderName, out correlationIdHeader))
            {
                correlationId = correlationIdHeader.FirstOrDefault();
            }
            if (String.IsNullOrWhiteSpace(correlationId))
            {
                correlationId = Guid.NewGuid().ToString("N");
            }

            if (context.Request.Headers.TryGetValue(HttpCorrelation.RequestOriginHeaderName, out originHeader))
            {
                HttpCorrelation.SetRequestOrigin(originHeader.FirstOrDefault());
            }

            HttpCorrelation.SetRequestCorrelationId(correlationId);
        }

        private readonly RequestDelegate _next;
        #endregion
    }
}
