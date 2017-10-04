using System;
using System.Threading;

namespace WpfHelpers.Collections
{
    /// <summary>
    /// Object pool with limit
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LimitedObjectPool<T> : ObjectPool<T>
    {
        private SemaphoreSlim _semaphore;
        private CancellationTokenSource cts;

        public uint Maximum { get; }

        public LimitedObjectPool(uint maximum, Func<T> objectGenerator) : base(objectGenerator)
        {
            Maximum = maximum;
            _semaphore = new SemaphoreSlim((int)maximum, (int)maximum);
            cts = new CancellationTokenSource();
        }

        public void Cancel()
        {
            cts.Cancel();
        }

        public override T GetObject()
        {
            try
            {
                _semaphore.Wait(cts.Token);
                try
                {
                    return base.GetObject();
                }
                catch (Exception)
                {
                    throw;
                }

            }
            catch (OperationCanceledException)
            {
                return default(T);
            }
        }

        public override void PutObject(T item)
        {
            base.PutObject(item);
            _semaphore.Release();
        }
    }
}