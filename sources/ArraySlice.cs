using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LuaByteSharp
{
    internal struct ArraySlice<T> : IList<T>, IReadOnlyList<T>
    {
        private T[] _array;

        public T[] Array => _array;

        public int Count { get; }
        public bool IsReadOnly { get; }
        public int Offset { get; }

        public ArraySlice(T[] array, int offset, int length)
        {
            _array = array;
            Offset = offset;
            Count = length;
            IsReadOnly = false;
        }

        public ArraySlice(T[] array) : this(array, 0, array.Length)
        {
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Array.Skip(Offset).Take(Count).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public T this[int index]
        {
            get => Array[Offset + index];
            set => Array[Offset + index] = value;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            System.Array.Copy(Array, Offset + arrayIndex, array, arrayIndex, Count);
        }

        public ArraySlice<T> Move(int newOffset)
        {
            return MoveAndResize(newOffset, Count);
        }

        public ArraySlice<T> Resize(int newSize)
        {
            return MoveAndResize(0, newSize);
        }

        public ArraySlice<T> MoveAndResize(int newOffset, int newSize, bool resizeBaseArray = false)
        {
            if (resizeBaseArray && Offset + newOffset + newSize > Array.Length)
            {
                System.Array.Resize(ref _array, newOffset + newSize);
            }
            return new ArraySlice<T>(Array, Offset + newOffset, newSize);
        }

        public T[] ToArray()
        {
            var array = new T[Count];
            System.Array.Copy(Array, Offset, array, 0, Count);
            return array;
        }

        public void SetAll(T[] source, int sourceIndex, int index, int count, bool resizeBaseArray = false)
        {
            if (resizeBaseArray && Offset + index + count > Array.Length)
            {
                System.Array.Resize(ref _array, Offset + index + count);
            }
            System.Array.Copy(source, sourceIndex, Array, Offset + index, count);
        }

        public void SetAll(ArraySlice<T> source, int sourceIndex, int index, int count, bool resizeBaseArray = false)
        {
            SetAll(source.Array, source.Offset + sourceIndex, index, count, resizeBaseArray);
        }

        bool ICollection<T>.Contains(T item)
        {
            throw new NotSupportedException();
        }

        int IList<T>.IndexOf(T item)
        {
            throw new NotSupportedException();
        }

        void ICollection<T>.Add(T item)
        {
            throw new NotSupportedException();
        }

        void ICollection<T>.Clear()
        {
            throw new NotSupportedException();
        }

        bool ICollection<T>.Remove(T item)
        {
            throw new NotSupportedException();
        }

        void IList<T>.Insert(int index, T item)
        {
            throw new NotSupportedException();
        }

        void IList<T>.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }
    }
}