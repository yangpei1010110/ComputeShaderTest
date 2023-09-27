#nullable enable
using System;

namespace Resource.Scripts.Tools
{
    public class ArrayTree<T>
    {
        public T[] _arr;
        public int Size  => _arr.Length;
        public int depth { get; private set; }

        public ref T this[in int index] => ref _arr[index];

        public ArrayTree(in int depth = 0)
        {
            _arr = new T[GetTotalSize(depth)];
        }

        public int Left(in int current)
        {
            if (current >= _arr.Length)
            {
                return -1;
            }
            else
            {
                return current * 2 + 1;
            }
        }

        public int Right(in int current)
        {
            if (current >= _arr.Length)
            {
                return -1;
            }
            else
            {
                return current * 2 + 2;
            }
        }

        public int LeftAndResize(in int current)
        {
            if (current >= _arr.Length)
            {
                return -1;
            }

            var result = Left(current);
            while (result >= _arr.Length)
            {
                ResizeNextDepth();
            }

            return result;
        }

        public int RightAndResize(in int current)
        {
            if (current >= _arr.Length)
            {
                return -1;
            }

            var result = Right(current);
            while (result >= _arr.Length)
            {
                ResizeNextDepth();
            }

            return result;
        }

        public int Parent(in int current)
        {
            return (current - 1) / 2;
        }

        public bool IsNull(in int index)
        {
            return index >= _arr.Length || _arr[index] == null || _arr[index]!.Equals(default(T));
        }

        private void ResizeNextDepth()
        {
            int newSize = GetTotalSize(depth + 1);
            Array.Resize(ref _arr, newSize);
            depth++;
        }

        private static int GetTotalSize(in int dep)
        {
            int result = 0;
            for (int i = 0; i <= dep; i++)
            {
                result += 1 << i;
            }

            return result;
        }
    }
}