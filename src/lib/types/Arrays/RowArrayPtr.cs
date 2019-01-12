using System;
using System.Runtime.CompilerServices;

namespace liblinear {
    public class RowArrayPtr<T> {
        private T[] _array;
        private int _offset;

        private int _end;

        private int _length;

        public int Length { get { return _length; } }

        public static RowArrayPtr<T> Empty () {
            return new RowArrayPtr<T> ();
        }

        private RowArrayPtr () {
            _array = new T[1];
            _end = _length = _offset = 0;
        }

        public void setOffset (int offset) {
            //Console.WriteLine("Setting offset {0} {1} {2} ", _array.Length, offset, _length);
            if (offset < 0 || offset > _array.Length) throw new ArgumentException ("offset must be between 0 and the length of the array");
            _offset = offset;
            _length = offset - _end;
        }

        public RowArrayPtr (T[] array, int offset, int end) {
            //Console.WriteLine("Creating arrays ptr {0} {1} {2}", array.Length, offset, end);
            _array = array;
            setOffset (offset);
            //System.Console.WriteLine("Offset {0} End {1}, array length {2}", offset, end, _array.Length);
            if (end < offset || end > _array.Length) throw new ArgumentException ("end must be between offset and the length of the array");
            _end = end;
            _length = (_end - offset) + 1;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public T get (int index) {
            //Console.WriteLine("Get Index {0} {1}", _array.Length, index);
            return _array[_offset + index];
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public void set (int index, T value) {
            throw new NotImplementedException ("Unsafe operation denied");
            //_array[_offset + index] = value;
        }
    }
}