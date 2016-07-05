using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ServiceFabric.Http.Client {
   public sealed class CircuitBreakerHttpMessageHandler : DelegatingHandler {
      private readonly Int32 m_failuresToOpen;
      private readonly TimeSpan m_timeToStayOpen;

      private const Int32 c_evictionScanFrequency = 100;
      private readonly TimeSpan m_evictionStaleTime = TimeSpan.FromMinutes(1);
      private Int32 m_evictionScan = 0;    // 0 to c_evictionScanFrequency-1
      private readonly SortedList<Uri, UriCircuitBreaker> m_pool =
         new SortedList<Uri, UriCircuitBreaker>(new UriComparer());

      private sealed class UriCircuitBreaker {
         private Int32 m_failureCount = 0;
         public DateTime LastAttempt = DateTime.UtcNow;

         public UriCircuitBreaker() { }
         public void ThrowIfOpen(Int32 failuresToOpen, TimeSpan timetoStayOpen) {
            lock (this) {
               if (m_failureCount < failuresToOpen) return;
               if (LastAttempt.Add(timetoStayOpen) < DateTime.UtcNow) return;
               throw new InvalidOperationException();
            }
         }
         public void ReportAttempt(Boolean succeeded, Int32 failuresToOpen) {
            lock (this) {
               LastAttempt = DateTime.UtcNow;
               if (succeeded) m_failureCount = 0; // Successful call, reset count
               else {
                  if (m_failureCount < failuresToOpen) m_failureCount++;   // Threshold reached, open breaker
               }
            }
         }
         public override string ToString() {
            return $"Failures={m_failureCount}, Last Attempt={LastAttempt}";
         }
      }

      public CircuitBreakerHttpMessageHandler(Int32 failuresToOpen, TimeSpan timeToStayOpen, HttpMessageHandler innerHandler = null) : base(innerHandler ?? new HttpClientHandler()) {
         m_failuresToOpen = failuresToOpen;
         m_timeToStayOpen = timeToStayOpen;
      }

      protected override void Dispose(bool disposing) => base.Dispose(disposing);

      protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
         UriCircuitBreaker ucb = GetCircuitBreaker(request.RequestUri);
         ucb.ThrowIfOpen(m_failuresToOpen, m_timeToStayOpen);
         // If we get here, we're closed
         try {
            HttpResponseMessage t = await base.SendAsync(request, cancellationToken);
            ucb.ReportAttempt(t.IsSuccessStatusCode, m_failuresToOpen);
            return t;
         }
         catch (Exception /*ex*/) { ucb.ReportAttempt(false, m_failuresToOpen); throw; }
      }

      private UriCircuitBreaker GetCircuitBreaker(Uri uri) {
         uri = new Uri(uri.GetLeftPart(UriPartial.Path));
         Monitor.Enter(m_pool);
         if ((m_evictionScan = (m_evictionScan + 1) % c_evictionScanFrequency) == 0) {
            var staleUris = from kvp in m_pool
                            where kvp.Value.LastAttempt.Add(m_evictionStaleTime) < DateTime.UtcNow
                            select kvp.Key;
            foreach (var staleUri in staleUris) m_pool.Remove(staleUri);
         }
         UriCircuitBreaker ucb;
         if (!m_pool.TryGetValue(uri, out ucb)) m_pool.Add(uri, ucb = new UriCircuitBreaker());
         Monitor.Exit(m_pool);
         return ucb;
      }

      private sealed class UriComparer : IComparer<Uri> {
         public int Compare(Uri uri1, Uri uri2) {
            return Uri.Compare(uri1, uri2, UriComponents.SchemeAndServer, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase);
         }
      }
   }
}