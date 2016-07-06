using System;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ServiceFabric.Http.Utilities
{
    public sealed class ReliableCollectionRetry
    {
        private readonly int maxRetries, delayMilliseconds, maxDelayMilliseconds;

        public ReliableCollectionRetry(int maxRetries = 3, int delayMilliseconds = 10, int maxDelayMilliseconds = 100)
        {
            this.maxRetries = maxRetries;
            this.delayMilliseconds = delayMilliseconds;
            this.maxDelayMilliseconds = maxDelayMilliseconds;
        }

        public async Task RunAsync(Func<Task> func, CancellationToken cancellationToken = default(CancellationToken))
        {
            ExponentialBackoff backoff = new ExponentialBackoff(this.maxRetries, this.delayMilliseconds, this.maxDelayMilliseconds);
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
