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
        public void RequestStart(HttpRequestMessage request, string origin, string correlationId)
        {
            RequestStart(request.RequestUri.OriginalString, request.Method.ToString(), origin, correlationId);
        }

        // A pair of events sharing the same name prefix with a "Start"/"Stop" suffix implicitly marks boundaries of an event tracing activity.
        // These activities can be automatically picked up by debugging and profiling tools, which can compute their execution time, child activities,
        // and other statistics.
        private const int RequestStartEventId = 5;
        [Event(RequestStartEventId, Level = EventLevel.Informational, Message = "Http request '{0}' started", Keywords = Keywords.Requests)]
        public void RequestStart(string requestName, string method, string origin, string correlationId)
        {
            correlationId = correlationId ?? string.Empty;
            origin = origin ?? string.Empty;

            WriteEvent(RequestStartEventId, requestName, method, origin, correlationId);
        }

        [NonEvent]
        public void RequestStop(HttpRequestMessage request, string origin, string correlationId, double duration)
        {
            RequestStop(request.RequestUri.OriginalString, request.Method.ToString(), origin, correlationId, duration);
        }

        private const int RequestStopEventId = 6;
        [Event(RequestStopEventId, Level = EventLevel.Informational, Message = "Http request '{0}' finished", Keywords = Keywords.Requests)]
        public void RequestStop(string requestName, string method, string origin, string correlationId, double duration)
        {
            correlationId = correlationId ?? string.Empty;
            origin = origin ?? string.Empty;

            WriteEvent(RequestStopEventId, requestName, method, origin, correlationId, duration);
        }

        [NonEvent]
        public void RequestFailed(HttpRequestMessage request, string origin, string correlationId, Exception exception)
        {
            RequestFailed(request.RequestUri.OriginalString, request.Method.ToString(), origin, correlationId, exception.ToString());
        }

        private const int RequestFailedEventId = 7;
        [Event(RequestFailedEventId, Level = EventLevel.Error, Message = "Http request '{0}' failed", Keywords = Keywords.Requests)]
        public void RequestFailed(string requestName, string method, string origin, string correlationId, string exception)
        {
            correlationId = correlationId ?? string.Empty;
            origin = origin ?? string.Empty;

            WriteEvent(RequestFailedEventId, requestName, method, origin, correlationId, exception);
        }
        #endregion
    }
}
