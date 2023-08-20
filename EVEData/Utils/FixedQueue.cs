namespace EVEDataUtils
{
    public class FixedQueue<T> : List<T>
    {
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

            if (sizeLimit != 0 && base.Count > sizeLimit)
            {
                Dequeue();
            }
        }

        public void ClearAll()
        {
            base.Clear();
        }

        public T Dequeue()
        {
            int position = base.Count - 1;
            var item = base[position];

            base.Remove(item);

            return item;
        }
    }
}