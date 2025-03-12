/* Copyright 2013-present MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver.Core.Async
{
    internal class AsyncQueue<T>
    {
        // fields
        private readonly object _lock = new object();
        private readonly Queue<T> _queue = new Queue<T>();
        private readonly Queue<UniTaskCompletionSource<T>> _awaiters = new Queue<UniTaskCompletionSource<T>>();

        // properties
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _queue.Count;
                }
            }
        }

        // methods
        public IEnumerable<T> DequeueAll()
        {
            lock (_lock)
            {
                while (_queue.Count > 0)
                {
                    yield return _queue.Dequeue();
                }
            }
        }

        public async UniTask<T> DequeueAsync(CancellationToken cancellationToken)
        {
            UniTaskCompletionSource<T> awaiter;
            lock (_lock)
            {
                if (_queue.Count > 0)
                {
                    return _queue.Dequeue();
                }
                else
                {
                    awaiter = new UniTaskCompletionSource<T>();
                    _awaiters.Enqueue(awaiter);
                }
            }

            using (cancellationToken.Register(() => awaiter.TrySetCanceled(), useSynchronizationContext: false))
            {
                return await awaiter.Task;
            }
        }

        public void Enqueue(T item)
        {
            UniTaskCompletionSource<T> awaiter = null;
            lock (_lock)
            {
                if (_awaiters.Count > 0)
                {
                    awaiter = _awaiters.Dequeue();
                }
                else
                {
                    _queue.Enqueue(item);
                }
            }

            if (awaiter != null)
            {
                awaiter.TrySetResult(item);
            }
        }
    }
}
