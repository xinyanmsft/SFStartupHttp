using System;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ServiceFabric.Http.Utilities
{
    /// <summary>
    /// When working with Service Fabric reliable collection, it's very important to always catch
    /// TimeoutException and FabricTransientException, and retry the operation. ReliableCollectionRetry 
    /// makes it easy to implement such pattern. Usage:
    ///     var retry = new ReilableCollectionRetry();
    ///     await retry.RunAsync(async ()=>
    ///     {
    ///        // work with reliable collection
    ///     }, cancellationToken);
    /// </summary>
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
