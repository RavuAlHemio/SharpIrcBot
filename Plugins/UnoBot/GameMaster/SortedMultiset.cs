using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SharpIrcBot.Plugins.UnoBot.GameMaster
{
    public class SortedMultiset<T> : ISet<T>
    {
        private SortedDictionary<T, int> _backingDict;

        public SortedMultiset()
        {
            _backingDict = new SortedDictionary<T, int>();
        }

        public SortedMultiset(IEnumerable<T> other)
        {
            _backingDict = new SortedDictionary<T, int>();
            this.UnionWith(other);
        }

        #region ISet implementation

        public bool Add(T item)
        {
            if (_backingDict.ContainsKey(item))
            {
                ++_backingDict[item];
            }
            else
            {
                _backingDict[item] = 1;
            }

            // pretend like the item was never there
            return true;
        }

        public void UnionWith(IEnumerable<T> other)
        {
            foreach (var value in other)
            {
                Add(value);
            }
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            IntersectWith(new SortedMultiset<T>(other));
        }

        public void IntersectWith(SortedMultiset<T> other)
        {
            var newBackingDict = new SortedDictionary<T, int>();

            foreach (var item in _backingDict)
            {
                if (other._backingDict.ContainsKey(item.Key))
                {
                    int myCount = item.Value;
                    int theirCount = other._backingDict[item.Key];

                    newBackingDict[item.Key] = Math.Min(myCount, theirCount);
                }
            }

            _backingDict = newBackingDict;
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            ExceptWith(new SortedMultiset<T>(other));
        }

        public void ExceptWith(SortedMultiset<T> other)
        {
            var newBackingDict = new SortedDictionary<T, int>();

            foreach (var item in _backingDict)
            {
                if (other._backingDict.ContainsKey(item.Key))
                {
                    int myCount = item.Value;
                    int theirCount = other._backingDict[item.Key];

                    if (myCount - theirCount <= 0)
                    {
                        // don't add this item at all
                        continue;
                    }

                    newBackingDict[item.Key] = myCount - theirCount;
                }
            }

            _backingDict = newBackingDict;
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            // a symdiff b = (a union b) setminus (a intersect b)

            var union = new SortedMultiset<T>(this);
            union.UnionWith(other);

            var intersection = new SortedMultiset<T>(this);
            intersection.IntersectWith(other);

            union.ExceptWith(intersection);
            _backingDict = union._backingDict;
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            return IsSubsetOf(new SortedMultiset<T>(other));
        }

        public bool IsSubsetOf(SortedMultiset<T> other)
        {
            foreach (var myItem in _backingDict)
            {
                // if the other set doesn't contain each of my items or contains less of one of the items, it's not a subset
                if (!other._backingDict.ContainsKey(myItem.Key) || other._backingDict[myItem.Key] < myItem.Value)
                {
                    return false;
                }
            }

            return true;
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return IsSupersetOf(new SortedMultiset<T>(other));
        }

        public bool IsSupersetOf(SortedMultiset<T> other)
        {
            // subset and superset are opposite
            return other.IsSubsetOf(this);
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            return IsProperSupersetOf(new SortedMultiset<T>(other));
        }

        public bool IsProperSupersetOf(SortedMultiset<T> other)
        {
            return IsSupersetOf(other) && !SetEquals(other);
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            return IsProperSubsetOf(new SortedMultiset<T>(other));
        }

        public bool IsProperSubsetOf(SortedMultiset<T> other)
        {
            return IsSubsetOf(other) && !SetEquals(other);
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            return Overlaps(new SortedMultiset<T>(other));
        }

        public bool Overlaps(SortedMultiset<T> other)
        {
            return _backingDict.Keys.Any(other._backingDict.ContainsKey);
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            return SetEquals(new SortedMultiset<T>(other));
        }

        public bool SetEquals(SortedMultiset<T> other)
        {
            foreach (var myItem in _backingDict)
            {
                if (!other._backingDict.ContainsKey(myItem.Key) || other._backingDict[myItem.Key] != myItem.Value)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region ICollection implementation

        void ICollection<T>.Add(T item)
        {
            Add(item);
        }

        public void Clear()
        {
            _backingDict.Clear();
        }

        public bool Contains(T item)
        {
            return _backingDict.ContainsKey(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            foreach (var item in _backingDict)
            {
                for (int i = 0; i < item.Value; ++i)
                {
                    array[arrayIndex] = item.Key;
                    ++arrayIndex;
                }
            }
        }

        public bool Remove(T item)
        {
            if (_backingDict.ContainsKey(item))
            {
                int count = _backingDict[item];
                Debug.Assert(count >= 1);
                if (count <= 1)
                {
                    _backingDict.Remove(item);
                }
                else
                {
                    _backingDict[item] = count - 1;
                }
                return true;
            }
            return false;
        }

        public int Count
        {
            get
            {
                return _backingDict.Values
                    .Sum(v => (int?)v) ?? 0;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        #endregion

        #region IEnumerable implementation

        protected IEnumerable<T> GetLinqEnumerable()
        {
            foreach (var kvp in _backingDict)
            {
                for (int i = 0; i < kvp.Value; ++i)
                {
                    yield return kvp.Key;
                }
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return GetLinqEnumerable().GetEnumerator();
        }

        #endregion

        #region IEnumerable implementation

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        public int this[T t]
        {
            get
            {
                return _backingDict[t];
            }
        }
    }
}
