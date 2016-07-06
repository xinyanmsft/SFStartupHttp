using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ServiceFabric.Http.Utilities
{
    /// <summary>
    /// Usage:
    /// ExponentialBackoff backoff = new ExponentialBackoff(3, 10, 100);
    /// retry:
    /// try {
    ///        // ...
    /// }
    /// catch (Exception ex) {
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
    }
}
