using System.Collections.Specialized;

namespace EVEDataUtils
{
    public class BindingQueue<T> : List<T>, INotifyCollectionChanged
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private int sizeLimit = 0;

        public void SetSizeLimit(int size)
        {
            if (size >= 0)
            {
                sizeLimit = size;
            }
        }

        public void Enqueue(T item)
        {
            base.Insert(0, item);

            if (CollectionChanged != null)
            {
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
            }

            if (sizeLimit != 0 && base.Count > sizeLimit)
            {
                Dequeue();
            }
        }

        public void ClearAll()
        {
            base.Clear();
            if (CollectionChanged != null)
            {
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        public T Dequeue()
        {
            int position = base.Count - 1;
            var item = base[position];

            if (CollectionChanged != null)
            {
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, position));
            }

            base.Remove(item);

            return item;
        }
    }
}