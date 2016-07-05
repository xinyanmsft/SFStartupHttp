using System;
using System.Diagnostics.Tracing;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.ServiceFabric.Http.Client
{
    [EventSource(Name = "Microsoft-ServiceFabric-Http-Client")]
    internal sealed class HttpClientEventSource : EventSource
    {
        public static readonly HttpClientEventSource Current = new HttpClientEventSource();

        static HttpClientEventSource()
        {
            // A workaround for the problem where ETW activities do not get tracked until Tasks infrastructure is initialized.
            // This problem will be fixed in .NET Framework 4.6.2.
            Task.Run(() => { });
        }

        // Instance constructor is private to enforce singleton semantics
        private HttpClientEventSource() : base() { }

        #region Keywords
        // Event keywords can be used to categorize events. 
        // Each keyword is a bit flag. A single event can be associated with multiple keywords (via EventAttribute.Keywords property).
        // Keywords must be defined as a public class named 'Keywords' inside EventSource that uses them.
        public static class Keywords
        {
            public const EventKeywords Requests = (EventKeywords)0x1L;
        }
        #endregion

        #region Events

        [NonEvent]
        public void HttpRequestStart(HttpRequestMessage request, string originServiceName, string correlationId)
        {
            HttpRequestStart(request.RequestUri.OriginalString, request.Method.ToString(), originServiceName, correlationId);
        }

        // A pair of events sharing the same name prefix with a "Start"/"Stop" suffix implicitly marks boundaries of an event tracing activity.
        // These activities can be automatically picked up by debugging and profiling tools, which can compute their execution time, child activities,
        // and other statistics.
        private const int HttpRequestStartEventId = 5;
        [Event(HttpRequestStartEventId, Level = EventLevel.Informational, Message = "Http request '{0}' started", Keywords = Keywords.Requests)]
        public void HttpRequestStart(string requestName, string method, string originServiceName, string correlationId)
        {
            correlationId = correlationId ?? string.Empty;
            originServiceName = originServiceName ?? string.Empty;

            WriteEvent(HttpRequestStartEventId, requestName, method, originServiceName, correlationId);
        }

        [NonEvent]
        public void HttpRequestStop(HttpRequestMessage request, string originServiceName, string correlationId, double duration)
        {
            HttpRequestStop(request.RequestUri.OriginalString, request.Method.ToString(), originServiceName, correlationId, duration);
        }

        private const int HttpRequestStopEventId = 6;
        [Event(HttpRequestStopEventId, Level = EventLevel.Informational, Message = "Http request '{0}' finished", Keywords = Keywords.Requests)]
        public void HttpRequestStop(string requestName, string method, string originServiceName, string correlationId, double duration)
        {
            correlationId = correlationId ?? string.Empty;
            originServiceName = originServiceName ?? string.Empty;

            WriteEvent(HttpRequestStopEventId, requestName, method, originServiceName, correlationId, duration);
        }

        [NonEvent]
        public void HttpRequestFailed(HttpRequestMessage request, string originServiceName, string correlationId, Exception exception)
        {
            HttpRequestFailed(request.RequestUri.OriginalString, request.Method.ToString(), originServiceName, correlationId, exception.ToString());
        }

        private const int HttpRequestFailedEventId = 7;
        [Event(HttpRequestFailedEventId, Level = EventLevel.Error, Message = "Http request '{0}' failed", Keywords = Keywords.Requests)]
        public void HttpRequestFailed(string requestName, string method, string originServiceName, string correlationId, string exception)
        {
            correlationId = correlationId ?? string.Empty;
            originServiceName = originServiceName ?? string.Empty;

            WriteEvent(HttpRequestFailedEventId, requestName, method, originServiceName, correlationId, exception);
        }
        #endregion
    }
}
