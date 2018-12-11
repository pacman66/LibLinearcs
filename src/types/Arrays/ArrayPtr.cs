using System;
using System.Runtime.CompilerServices;

namespace liblinearcs {
    public class ArrayPtr<T> {

        private T[] _array;
        private int _offset;

        public void setOffset (int offset) {
            if (offset < 0 || offset > _array.Length) throw new ArgumentException ("offset must be between 0 and the length of the array");
            _offset = offset;
        }

        public ArrayPtr (T[] array, int offset) {
            _array = array;
            setOffset (offset);
        }

        public T get (int index) {
            return _array[_offset + index];
        }

        public void set (int index, T value) {
            _array[_offset + index] = value;
        }
    }
}