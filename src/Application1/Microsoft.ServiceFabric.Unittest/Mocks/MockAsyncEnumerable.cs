// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.ServiceFabric.Unittest.Mocks
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Data;

    /// <summary>
    /// Simple wrapper for asynchronous IEnumerable of T.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MockAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        private IEnumerable<T> enumerable;

        public MockAsyncEnumerable(IEnumerable<T> enumerable)
        {
            this.enumerable = enumerable;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator()
        {
            return new MockAsyncEnumerator<T>(this.enumerable.GetEnumerator());
        }
    }

    public class MockAsyncEnumerator<T2> : IAsyncEnumerator<T2>
    {
        private readonly IEnumerator<T2> enumerator;

        public MockAsyncEnumerator(IEnumerator<T2> enumerator)
        {
            this.enumerator = enumerator;
        }

        public T2 Current
        {
            get { return this.enumerator.Current; }
        }

        public void Dispose()
        {
            this.enumerator.Dispose();
        }

        public Task<bool> MoveNextAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(this.enumerator.MoveNext());
        }

        public void Reset()
        {
            this.enumerator.Reset();
        }
    }
}