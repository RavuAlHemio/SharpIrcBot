using System;
using System.Collections.Generic;
using System.Linq;

namespace UnoBot.GameMaster
{
    public class Pile<T> : IList<T>
    {
        private List<T> _backingList;

        public Pile()
        {
            _backingList = new List<T>();
        }

        public Pile(IEnumerable<T> what)
        {
            _backingList = new List<T>(what);
        }

        #region IList implementation

        public int IndexOf(T item)
        {
            return _backingList.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            _backingList.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _backingList.RemoveAt(index);
        }

        public T this[int index]
        {
            get
            {
                return _backingList[index];
            }
            set
            {
                _backingList[index] = value;
            }
        }

        #endregion

        #region ICollection implementation

        void ICollection<T>.Add(T item)
        {
            _backingList.Add(item);
        }

        public void Clear()
        {
            _backingList.Clear();
        }

        public bool Contains(T item)
        {
            return _backingList.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _backingList.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return _backingList.Remove(item);
        }

        public int Count
        {
            get
            {
                return _backingList.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return ((ICollection<T>)_backingList).IsReadOnly;
            }
        }

        #endregion

        #region IEnumerable implementation

        public IEnumerator<T> GetEnumerator()
        {
            return _backingList.GetEnumerator();
        }

        #endregion

        #region IEnumerable implementation

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((System.Collections.IEnumerable)_backingList).GetEnumerator();
        }

        #endregion

        public void AddRange(IEnumerable<T> items)
        {
            _backingList.AddRange(items);
        }

        public void Push(T item)
        {
            _backingList.Add(item);
        }

        public List<T> PeekMany(int maxCount)
        {
            if (maxCount < 0)
            {
                throw new ArgumentOutOfRangeException("maxCount", maxCount, "maxCount must be at least 0");
            }
            if (maxCount >= _backingList.Count)
            {
                // return the whole thing
                return new List<T>(_backingList);
            }

            return new List<T>(_backingList.Skip(_backingList.Count - maxCount));
        }

        public T Peek()
        {
            if (_backingList.Count == 0)
            {
                throw new InvalidOperationException("pile is empty");
            }
            return PeekMany(1)[0];
        }

        public List<T> DrawMany(int maxCount)
        {
            var peeked = PeekMany(maxCount);

            // remove them from the end of the backing list
            _backingList.RemoveRange(_backingList.Count - peeked.Count, peeked.Count);

            return peeked;
        }

        public List<T> DrawAll()
        {
            var ret = new List<T>(_backingList);
            _backingList.Clear();
            return ret;
        }

        public T Draw()
        {
            if (_backingList.Count == 0)
            {
                throw new InvalidOperationException("pile is empty");
            }
            return DrawMany(1)[0];
        }

        public void Shuffle(Random randomizer)
        {
            Shuffle(_backingList, randomizer);
        }

        public static void Shuffle(IList<T> list, Random randomizer)
        {
            // Fisher-Yates Shuffle (Knuth Shuffle)
            for (int i = 0; i < list.Count - 1; ++i)
            {
                // i <= j < count
                int j = randomizer.Next(i, list.Count);

                // swap
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }
    }
}
