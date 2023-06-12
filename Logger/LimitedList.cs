using System;
using System.Collections.ObjectModel;

namespace Logging
{
    internal class LimitedList<T> : Collection<T>
    {
        private int _capacity;

        public LimitedList(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            _capacity = capacity;
        }

        protected override void InsertItem(int index, T item)
        {
            if (Count >= _capacity)
            {
                RemoveAt(0);
                index--;
            }

            base.InsertItem(index, item);
        }
    }
}
