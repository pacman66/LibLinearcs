using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace liblinear {

    static class Helper {

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        static public void swap<T> (ref T x, ref T y) {
            T t;
            t = x;
            x = y;
            y = t;
        }

        /// <summary>
        /// Perform a deep Copy of a single object.  (Untested on arrays)
        /// </summary>
        /// <typeparam name="T">The type of object being copied.</typeparam>
        /// <param name="source">The object instance to copy.</param>
        /// <returns>The copied object.</returns>
        public static T Clone<T> (T source) {
            if (!typeof (T).IsSerializable) {
                throw new ArgumentException ("The type must be serializable.", "source");
            }

            // Don't serialize a null object, simply return the default for that object
            if (Object.ReferenceEquals (source, null)) {
                return default (T);
            }

            IFormatter formatter = new BinaryFormatter ();
            Stream stream = new MemoryStream ();
            using (stream) {
                formatter.Serialize (stream, source);
                stream.Seek (0, SeekOrigin.Begin);
                return (T) formatter.Deserialize (stream);
            }
        }

        /// <summary>
        /// Create a clone of an array.false (Note shallow copy of the array)
        /// </summary>
        public static void Clone<T> (ref T[] dst, T[] src, int n) {
            dst = new T[n];

            Array.Copy (src, 0, dst, 0, n);
        }

        static public int compare_double (double a, double b) {
            return (double.Equals (a, b)) ? 0 : (a > b) ? -1 : 0;
        }

        static public int compare_double_orig (double a, double b) {
            return (a > b) ? -1 : (a < b) ? 1 : 0;
        }

        static public int GETI (Problem prob, int i) {
            return ((int) prob.y[i]);
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        static public void swap (int[] array, int idxA, int idxB) {
            int temp = array[idxA];
            array[idxA] = array[idxB];
            array[idxB] = temp;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        static public void swap (double[] array, int idxA, int idxB) {
            double temp = array[idxA];
            array[idxA] = array[idxB];
            array[idxB] = temp;
        }

        public static int maxSort = 0;

        public static void qsortd (double[] comparable, int left, int right) {
            //Console.WriteLine("array Length = "+ comparable.Length);
            if (comparable.Length < 20)
                bsortd (comparable);
            else
                quicksortd (comparable, left, right);
        }

        public static void quicksortd (double[] comparable, int left, int right) {

            if (left < right) {
                int i = left, j = right;
                double pivot = comparable[(left + right) / 2];
                while (i < j) {
                    while (comparable[i] > pivot) {
                        i++;
                    }
                    while (comparable[j] < pivot) {
                        j--;
                    }
                    if (i < j) {
                        // Swap
                        double t = comparable[i];
                        comparable[i] = comparable[j];
                        comparable[j] = t;

                        i++;
                        j--;
                    }
                }

                if (left < j) {
                    Helper.quicksortd (comparable, left, j - 1);
                }

                if (i < right) {
                    Helper.quicksortd (comparable, i + 1, right);
                }
            }
        }

        public static void bsortd (double[] comparable) {
            int len = comparable.Length;

            for (int i = len - 1; i > 0; i--)
                for (int j = i - 1; j > -1; j--)
                    if (comparable[i] > comparable[j]) {
                        double t = comparable[i];
                        comparable[i] = comparable[j];
                        comparable[j] = t;
                    }
        }

        public static void isortd (double[] comparable) {
            int len = comparable.Length;

            for (int i = 1; i < len; i++) {
                for (int j = i; j > 0 && comparable[j - 1] < comparable[j]; j--) {
                    double t = comparable[j - 1];
                    comparable[j - 1] = comparable[j];
                    comparable[j] = t;
                }
            }
        }

        public static void qsort<T> (T[] comparable, int left, int right, Func<T, T, int> comparator) {
            if (comparable.Length < 20)
                bsort<T> (comparable, comparator);
            else
                quicksort (comparable, left, right, comparator);
        }

        public static void quicksort<T> (T[] comparable, int left, int right, Func<T, T, int> comparator) {

            if (left < right) {
                int i = left, j = right;
                T pivot = comparable[(left + right) / 2];
                while (i < j) {
                    while (comparator (comparable[i], pivot) < 0) {
                        i++;
                    }
                    while (comparator (comparable[j], pivot) > 0) {
                        j--;
                    }
                    if (i < j) {
                        // Swap
                        Helper.swap (ref comparable[i], ref comparable[j]);

                        i++;
                        j--;
                    }
                }

                if (left < j) {
                    Helper.quicksort<T> (comparable, left, j - 1, comparator);
                }

                if (i < right) {
                    Helper.quicksort<T> (comparable, i + 1, right, comparator);
                }
            }
        }

        public static void bsort<T> (T[] comparable, Func<T, T, int> comparator) {
            int len = comparable.Length;
            for (int i = 0; i < len - 1; i++)
                for (int j = i; j < len; j++)
                    if (comparator (comparable[i], comparable[j]) > 0)
                        Helper.swap<T> (ref comparable[i], ref comparable[j]);
        }

    }
}