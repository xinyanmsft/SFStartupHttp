using Microsoft.AspNetCore.Http;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Application1.Frontend
{
    internal sealed class ServiceMiddleware
    {
        public ServiceMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext, ServiceMonitor monitor)
        {
            monitor.OnRequestStart(httpContext);

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                monitor.OnRequestException(httpContext, ex);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                monitor.OnRequestStop(httpContext, stopwatch.Elapsed);
            }
        }

        private readonly RequestDelegate _next;
    }
}
