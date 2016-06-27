using Microsoft.AspNetCore.Http;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Application1.UserSessionService
{
    internal class ServiceMiddleware
    {
        public ServiceMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext, ServiceWatchdog watchdog)
        {
            watchdog.OnRequestStart(httpContext);

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                watchdog.OnRequestException(httpContext, ex);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                watchdog.OnRequestStop(httpContext, stopwatch.Elapsed);
            }
        }

        private readonly RequestDelegate _next;
    }
}
