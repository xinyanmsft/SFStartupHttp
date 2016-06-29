using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ServiceFabric.Http.Utilities
{
    /// <summary>
    /// Usage:
    /// 
    /// ExponentialBackoff.Run(async () =>
    ///     {
    ///         // ...
    ///     }, cancellationToken);
    ///     
    /// ExponentialBackoff backoff = new ExponentialBackoff(3, 10, 100);
    /// retry:
    /// try {
    ///        // ...
    /// }
    /// catch (Exception ex) when(ex is TimeoutException || ex is FabricTransientException)
    /// {
    ///    await backoff.Delay(cancellationToken);
    ///    goto retry;
    /// }
    /// </summary>
    public struct ExponentialBackoff
    {
        private readonly int m_maxRetries, m_delayMilliseconds, m_maxDelayMilliseconds;
        private int m_retries, m_pow;

        public ExponentialBackoff(int maxRetries, int delayMilliseconds, int maxDelayMilliseconds)
        {
            m_maxRetries = maxRetries;
            m_delayMilliseconds = delayMilliseconds;
            m_maxDelayMilliseconds = maxDelayMilliseconds;
            m_retries = 0;
            m_pow = 1;
        }

        public Task Delay(CancellationToken cancellationToken)
        {
            if (m_retries == m_maxRetries)
            {
                throw new TimeoutException("Max retry attempts exceeded.");
            }
            ++m_retries;
            if (m_retries < 31)
            {
                m_pow = m_pow << 1; // m_pow = Pow(2, m_retries - 1)
            }
            int delay = Math.Min(m_delayMilliseconds * (m_pow - 1) / 2, m_maxDelayMilliseconds);
            return Task.Delay(delay, cancellationToken);
        }

        public static async Task Run(Func<Task> func, CancellationToken cancellationToken = default(CancellationToken), int maxRetries = 3, int delayMilliseconds = 10, int maxDelayMilliseconds = 100)
        {
            ExponentialBackoff backoff = new ExponentialBackoff(maxRetries, delayMilliseconds, maxDelayMilliseconds);
            retry:
            try
            {
                await func();
            }
            catch (Exception ex) when (ex is TimeoutException || ex is FabricTransientException)
            {
                await backoff.Delay(cancellationToken);
                goto retry;
            }
        }
    }
}
