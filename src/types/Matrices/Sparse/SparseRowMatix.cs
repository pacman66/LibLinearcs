using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using Microsoft.Extensions.Logging;

namespace liblinearcs {
    public class SparseRowMatrix : SparseMatrix {
        int[] rowPtr = null;
        int[] colPtr = null;
        double[] val = null;
        static int ARRAYINCREMENT = 1000;
        int rowCount = 0;
        int colCount = 0;
        ILogger<SparseRowMatrix> _logger;

        public SparseRowMatrix (int rowCount, int colCount) {
            ContractAssertions.Requires<ArgumentOutOfRangeException> (rowCount >= 0, "RowCount must be positive");
            ContractAssertions.Requires<ArgumentOutOfRangeException> (colCount >= 0, "colCount must be positive");

            this.rowCount = rowCount;
            rowPtr = new int[rowCount + 1];
            this.colCount = colCount;
            colPtr = new int[0];
            val = new double[0];

            _logger = ApplicationLogging.CreateLogger<SparseRowMatrix> ();
        }

        public SparseRowMatrix (int rowCount, int colCount, int initvalues) {
            ContractAssertions.Requires<ArgumentOutOfRangeException> (rowCount >= 0, "RowCount must be positive");
            ContractAssertions.Requires<ArgumentOutOfRangeException> (colCount >= 0, "colCount must be positive");
            ContractAssertions.Requires<ArgumentOutOfRangeException> (initvalues >= 0, "initvalues must be positive");

            this.rowCount = rowCount;
            rowPtr = new int[rowCount + 1];
            this.colCount = colCount;
            colPtr = new int[initvalues];
            val = new double[initvalues];
        }

        public int NonZeroValueCount {
            get { return rowPtr[rowCount]; }
        }

        /// <summary>
        /// Number of rows in the Matrix
        /// </summary>
        /// <returns>The number of rows in the Matrix</returns>
        [ReliabilityContract (Consistency.WillNotCorruptState, Cer.MayFail)]
        public int RowCount () {
            return rowCount;
        }

        /// <summary>
        /// Number of Columns in the Matrix
        /// </summary>
        /// <returns>The number of Columns in the Matrix</returns>
        [ReliabilityContract (Consistency.WillNotCorruptState, Cer.MayFail)]
        public int ColCount () {
            return colCount;
        }

        /// <summary>
        /// The length (number of values) in the row
        /// </summary>
        /// <param name="row">The row</param>
        /// <returns>The requested length of the row.</returns>
        /// <remarks>Not range-checked.</remarks>
        [ReliabilityContract (Consistency.MayCorruptProcess, Cer.MayFail)]
        public int rowLength (int row) {
            return rowPtr[row + 1] - rowPtr[row];
        }

        /// <summary>
        /// The length (number of values) in the row
        /// </summary>
        /// <param name="row">The row</param>
        /// <returns>/// The requested length of the row.</returns>
        /// <remarks>
        /// Not range-checked - Hence low consistency gaurantee.
        /// If the row is empty will return one less than the start of the row.
        /// </remarks>
        [ReliabilityContract (Consistency.MayCorruptProcess, Cer.MayFail)]
        public int RowEnd (int row) {
            return rowPtr[row + 1] - 1;
        }

        /// <summary>
        /// The length (number of values) in the row
        /// </summary>
        /// <param name="row">The row</param>
        /// <returns>The requested length of the row.</returns>
        /// <remarks>range-checked.</remarks>
        [ReliabilityContract (Consistency.WillNotCorruptState, Cer.MayFail)]
        public int RowLengthSafe (int row) {
            ContractAssertions.Requires<ArgumentOutOfRangeException> (row >= 0 && row <= rowCount, "Array Index out of bounds");
            return rowPtr[row + 1] - rowPtr[row];
        }

        /// <summary>
        /// The length (number of values) in the row
        /// </summary>
        /// <param name="row">The row</param>
        /// <returns>/// The requested length of the row.</returns>
        /// <remarks>
        /// Range checked.
        /// If the row is empty will return one less than the start of the row.
        /// </remarks>
        [ReliabilityContract (Consistency.WillNotCorruptState, Cer.MayFail)]
        public int rowEndSafe (int row) {
            ContractAssertions.Requires<ArgumentOutOfRangeException> (row >= 0 && row <= rowCount, "Array Index out of bounds");
            return rowPtr[row + 1] - 1;
        }

        public double this [int i, int j] {
            get { return this.At (i, j); }
            set { At (i, j, value); }
        }

        public double At (int i, int j) {
            ContractAssertions.Requires<ArgumentOutOfRangeException> (i >= 0 && i <= rowCount, "Array Index out of bounds");
            ContractAssertions.Requires<ArgumentOutOfRangeException> (j >= 0 && j <= colCount, "Array Index out of bounds");
            int index = Array.BinarySearch (colPtr, rowPtr[i], rowLength (i), j);
            //Console.WriteLine("index {0}", index);
            return (index < 0) ? 0.0 : val[index];
        }

        public void At (int i, int j, double value) {
            ContractAssertions.Requires<ArgumentOutOfRangeException> (i >= 0 && i <= rowCount, "Array Index out of bounds");
            ContractAssertions.Requires<ArgumentOutOfRangeException> (j >= 0 && j <= colCount, "Array Index out of bounds");
            int index = 0;
            index = Array.BinarySearch (colPtr, rowPtr[i], rowLength (i), j);
            //Console.WriteLine("after Binary search {0}", index);

            if (index < 0) {
                //Console.Write("Not Found - ");
                // TODO:  Need to insert value here
                // for efficiency should probably have a smaller array and merge periodically rather than all the time
                if (value == 0.0D) return;
                if (NonZeroValueCount == val.Length) {
                    // Need to resize the array as hit maximum size
                    //Console.Write("Resizing");
                    Array.Resize (ref val, val.Length + ARRAYINCREMENT);
                    Array.Resize (ref colPtr, colPtr.Length + ARRAYINCREMENT);
                }
                //Console.WriteLine("after Resize {0}-{1}", NonZeroValueCount, val.Length);
                // find where it is supposed to go

                index = (index * -1) - 1;
                //Console.WriteLine("rowPtr[rowCount]={0}, index={1} ",rowPtr[rowCount],index);
                //Console.WriteLine("after Closest {0}-{1}", rowPtr[rowCount-1], index);
                for (int t = rowPtr[rowCount]; t > index; t--) {
                    colPtr[t] = colPtr[t - 1];
                    val[t] = val[t - 1];
                }
                //Console.WriteLine("After Val Move {0}-{1}--{2}", i, rowPtr.Length, rowCount);
                for (int t = i + 1; t < rowCount + 1; t++)
                    rowPtr[t]++;
                //Console.WriteLine("After RowPtr Move {0}", index);
                colPtr[index] = j;
                val[index] = value;
            } else {
                //  If Value is zero then we must delete the entry
                if (value == 0) {
                    for (int t = i + 1; t < rowCount + 1; t++)
                        rowPtr[t]--;

                    Array.Copy (val, index + 1, val, index, rowPtr[rowCount] - index);
                    Array.Copy (colPtr, index + 1, colPtr, index, rowPtr[rowCount] - index);
                } else val[index] = value;
            }

        }

        public void print () {
            for (int i = 0; i < 10; i++) {
                for (int j = 0; j < 10; j++)
                    Console.Write (String.Format ("{0} ", At (i, j)));
                Console.WriteLine ();
            }
        }

        public SparseMatrix Transpose () {
            SparseRowMatrix trans = new SparseRowMatrix (colCount, rowCount);
            int[] rowCounts = new int[colCount];
            rowCounts.Initialize ();

            for (int i = 0; i < NonZeroValueCount; i++)
                rowCounts[colPtr[i]]++;

            int y = 0;
            int t = 0;
            for (int i = 0; i < colCount; i++) {
                trans.rowPtr[i] = y;
                t = y;
                y += rowCounts[i];
                rowCounts[i] = t;
            }
            trans.rowPtr[colCount] = y;

            trans.val = new double[y];
            trans.colPtr = new int[y];

            for (int i = 0; i < rowCount; i++) {
                for (int j = rowPtr[i]; j < rowPtr[i + 1]; j++) {
                    trans.colPtr[rowCounts[colPtr[j]]] = i;
                    trans.val[rowCounts[colPtr[j]]++] = val[j];
                }
            }

            return trans;
        }

        public double dot (int row, double[] s) {
            ContractAssertions.Requires<ArgumentOutOfRangeException> (row >= 0 && row <= rowCount, "Array Index out of bounds");
            double ret = 0;
            int nextRowStart = rowPtr[row + 1], i;
            for (i = rowPtr[row] + 1; i < nextRowStart; i += 2) {
                ret += (s[colPtr[i]] * val[i] + s[colPtr[i - 1]] * val[i - 1]);
            }
            if (i == nextRowStart)
                ret += s[colPtr[i - 1]] * val[i - 1];

            //            for(int i = rowPtr[row]; i < rowPtr[row+1]; i++ ) 
            //                ret += s[colPtr[i]]*val[i];

            return (ret);
        }

        public void axpy (double a, int row, double[] y) {
            ContractAssertions.Requires<ArgumentOutOfRangeException> (row >= 0 && row <= rowCount, "Array Index out of bounds");
            int nextRowStart = rowPtr[row + 1];
            for (int i = rowPtr[row]; i < nextRowStart; i++)
                y[colPtr[i]] += a * val[i];
        }

        public double nrm2_sq (int row) {
            ContractAssertions.Requires<ArgumentOutOfRangeException> (row >= 0 && row <= rowCount, "Array Index out of bounds");
            double ret = 0;
            int nextRowStart = rowPtr[row + 1], i;
            for (i = rowPtr[row] + 1; i < nextRowStart; i += 2) {
                ret += (val[i] * val[i] + val[i - 1] * val[i - 1]);
            }
            if (i == nextRowStart)
                ret += val[i - 1] * val[i - 1];

            // for(int i = rowPtr[row]; i < rowPtr[row+1]; i++) 
            //      ret += val[i] * val[i];

            return (ret);
        }

        public IEnumerable<SparseRowValue> getRow (int row) {
            ContractAssertions.Requires<ArgumentOutOfRangeException> (row >= 0 && row <= rowCount, "Array Index out of bounds");
            for (int i = rowPtr[row]; i < rowPtr[row + 1]; i++)
                yield return new SparseRowValue (colPtr[i], val[i]);
        }

        public RowArrayPtr<SparseRowValue> getRowPtr (int row) {
            ContractAssertions.Requires<ArgumentOutOfRangeException> (row >= 0 && row <= rowCount, "Array Index out of bounds");
            if (rowLength (row) > 0) {
                var v = getRow (row);

                return new RowArrayPtr<SparseRowValue> (v.ToArray (), 0, rowLength (row) - 1);
            } else return RowArrayPtr<SparseRowValue>.Empty ();
        }

        public void CopyRow (int toRow, SparseMatrix from, int fromRow) {
            SparseRowMatrix.CopyRow (this, toRow, (SparseRowMatrix) from, fromRow);
        }

        public static void CopyRow (SparseRowMatrix to, int row, SparseRowMatrix from, int fromRow) {
            ContractAssertions.Requires<ArgumentOutOfRangeException> (row >= 0 && row <= to.RowCount (), "Array Index out of bounds");
            ContractAssertions.Requires<ArgumentOutOfRangeException> (fromRow >= 0 && fromRow <= from.RowCount (), "Array Index out of bounds");
            ContractAssertions.Requires<ArgumentOutOfRangeException> (to.ColCount () == from.ColCount () && to.RowCount () >= from.RowCount (), "from.colCount > toColcount");
            int frl = from.rowLength (fromRow);

            if (frl == 0) return;

            //Console.WriteLine("Copyrow - "+ to.NonZeroValueCount+  " ," + to.val.Length +" ,"+ frl+" ,"+ (frl/ARRAYINCREMENT)  );

            if (to.NonZeroValueCount + frl > to.val.Length) {
                // Need to resize the array as hit maximum size
                Array.Resize (ref to.val, to.val.Length + (frl > ARRAYINCREMENT ? ((frl / ARRAYINCREMENT) + 1) * ARRAYINCREMENT : ARRAYINCREMENT));
                Array.Resize (ref to.colPtr, to.val.Length + (frl > ARRAYINCREMENT ? ((frl / ARRAYINCREMENT) + 1) * ARRAYINCREMENT : ARRAYINCREMENT));
            }

            if (to.rowPtr[row] == to.NonZeroValueCount) {
                // points to the end of the array so can just append all the rows
                int trow = to.rowPtr[row];

                for (int i = from.rowPtr[fromRow]; i < from.rowPtr[fromRow + 1]; i++) {
                    to.val[trow] = from.val[i];
                    to.colPtr[trow] = from.colPtr[i];
                    trow++;
                }

                for (int i = row + 1; i <= to.rowCount; i++)
                    to.rowPtr[i] = trow;
            } else {
                // Need to insert frl rows
                int trow = to.rowPtr[row];
                for (int t = to.rowPtr[to.rowCount]; t > trow; t--) {
                    to.val[t] = to.val[t - 1];
                    to.colPtr[t] = to.colPtr[t - 1];
                }

                for (int i = from.rowPtr[fromRow]; i < from.rowPtr[fromRow + 1]; i++) {
                    to.val[trow] = from.val[i];
                    to.colPtr[trow] = from.colPtr[i];
                    trow++;
                }

                for (int i = row + 1; i <= to.rowCount; i++)
                    to.rowPtr[i] += frl;
            }
        }

        private int _SequentialLoad = 0;

        public Boolean SequentialLoad {
            get { return (_SequentialLoad > 0); }
            set { _SequentialLoad += (value) ? +1 : -1; }
        }

        public void LoadRow (int row, int[] cols, double[] vals, int newRowLen) {
            bool checkSorted = true;
            for (int i = 1; i < newRowLen; i++)
                if (cols[i - 1] > cols[i]) checkSorted = false;

            if (!checkSorted) throw new ArgumentException ("Cols is not sorted");

            if (rowPtr[row] != rowPtr[row + 1] && _SequentialLoad == 0) throw new ArgumentException ("Row alreadys exists");

            if (NonZeroValueCount + newRowLen > val.Length) {
                // Need to resize the array as hit maximum size
                Array.Resize (ref val, val.Length + (newRowLen > ARRAYINCREMENT ? ((newRowLen / ARRAYINCREMENT) + 1) * ARRAYINCREMENT : ARRAYINCREMENT));
                Array.Resize (ref colPtr, val.Length + (newRowLen > ARRAYINCREMENT ? ((newRowLen / ARRAYINCREMENT) + 1) * ARRAYINCREMENT : ARRAYINCREMENT));
            }

            int trow = rowPtr[row];

            for (int i = 0; i < newRowLen; i++) {
                val[trow] = vals[i];
                colPtr[trow] = cols[i];
                trow++;
            }

            if (SequentialLoad) {
                rowPtr[row + 1] = trow;
            } else {
                for (int i = row + 1; i <= rowCount; i++)
                    rowPtr[i] = trow;
            }
        }
    }

}