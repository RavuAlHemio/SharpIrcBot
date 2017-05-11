using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace SharpIrcBot.Collections
{
    public sealed class RingBuffer<T> : IEnumerable<T>
    {
        private T[] _buffer;
        private int _nextIndex;
        private int _count;

        public RingBuffer(int bufferSize)
        {
            _buffer = new T[bufferSize];
            _nextIndex = 0;
            _count = 0;
        }

        public void Add(T value)
        {
            ++_count;
            if (_count > _buffer.Length)
            {
                _count = _buffer.Length;
            }

            _buffer[_nextIndex] = value;
            ++_nextIndex;
            if (_nextIndex >= _buffer.Length)
            {
                _nextIndex = 0;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            ImmutableArray<T>.Builder ret = ImmutableArray.CreateBuilder<T>(_count);

            if (_count < _buffer.Length)
            {
                // copy from the start
                ret.AddRange(_buffer, _count);
            }
            else
            {
                // first, copy the block from the next index to the end
                int tailLength = _buffer.Length - _nextIndex;
                for (int i = 0; i < tailLength; ++i)
                {
                    ret.Add(_buffer[_nextIndex + i]);
                }

                // next, copy from the beginning until before the next index
                ret.AddRange(_buffer, _nextIndex);
            }

            return ((IEnumerable<T>)ret.MoveToImmutable()).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
