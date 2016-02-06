using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace SharpIrcBot
{
    public class FilteringSetWrapper<T> : ISet<T>
    {
        [NotNull]
        private ISet<T> _innerSet;
        [NotNull]
        private Func<T, T> _transformFunc;

        public FilteringSetWrapper([NotNull] ISet<T> innerSet, [NotNull] Func<T, T> transformFunc)
        {
            _innerSet = innerSet;
            _transformFunc = transformFunc;
        }

        #region ISet implementation

        public bool Add(T item)
        {
            return _innerSet.Add(_transformFunc(item));
        }

        public void UnionWith(IEnumerable<T> other)
        {
            _innerSet.UnionWith(other.Select(_transformFunc));
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            _innerSet.IntersectWith(other.Select(_transformFunc));
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            _innerSet.ExceptWith(other.Select(_transformFunc));
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            _innerSet.SymmetricExceptWith(other.Select(_transformFunc));
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            return _innerSet.IsSubsetOf(other);
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return _innerSet.IsSupersetOf(other);
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            return _innerSet.IsProperSupersetOf(other);
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            return _innerSet.IsProperSubsetOf(other);
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            return _innerSet.Overlaps(other);
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            return _innerSet.SetEquals(other);
        }

        #endregion

        #region ICollection implementation

        void ICollection<T>.Add(T item)
        {
            Add(item);
        }

        public void Clear()
        {
            _innerSet.Clear();
        }

        public bool Contains(T item)
        {
            return _innerSet.Contains(_transformFunc(item));
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _innerSet.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return _innerSet.Remove(_transformFunc(item));
        }

        public int Count
        {
            get
            {
                return _innerSet.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return _innerSet.IsReadOnly;
            }
        }

        #endregion

        #region IEnumerable implementation

        public IEnumerator<T> GetEnumerator()
        {
            return _innerSet.GetEnumerator();
        }

        #endregion

        #region IEnumerable implementation

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}

