using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WpfHelpers.Collections
{
    /// <summary>
    /// Lite object pool
    /// </summary>
    /// <typeparam name="T">Type</typeparam>
    public class ObjectPool<T>
    {
        private readonly Func<T> _objectGenerator;
        protected readonly ConcurrentBag<T> Objects;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="objectGenerator">Function-creator</param>
        public ObjectPool(Func<T> objectGenerator)
        {
            if (objectGenerator == null) throw new ArgumentNullException(nameof(objectGenerator));
            Objects = new ConcurrentBag<T>();
            _objectGenerator = objectGenerator;
        }

        /// <summary>
        /// Gets object from pool
        /// </summary>
        /// <returns></returns>
        public virtual T GetObject()
        {
            T item;
            if (Objects.TryTake(out item)) return item;
            return _objectGenerator();
        }

        /// <summary>
        /// Returns object to pool
        /// </summary>
        /// <param name="item"></param>
        public virtual void PutObject(T item)
        {
            Objects.Add(item);
        }
    }
}
