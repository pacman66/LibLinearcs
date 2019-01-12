using System;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using Microsoft.Extensions.Logging;

namespace liblinear {

    public class SparseRowMatrixList : SparseMatrix{

        Dictionary<int, SparseRowValue[]> val = new Dictionary<int, SparseRowValue[]> ();

        int rowCount = 0;
        int colCount = 0;

        int _NonZeroValueCount = 0;
        ILogger<SparseRowMatrixList> _logger;

        public SparseRowMatrixList (int rowCount, int colCount) {
            ContractAssertions.Requires<ArgumentOutOfRangeException> (rowCount >= 0, "RowCount must be positive");
            ContractAssertions.Requires<ArgumentOutOfRangeException> (colCount >= 0, "colCount must be positive");
            this.rowCount = rowCount;
            this.colCount = colCount;

            _logger = ApplicationLogging.CreateLogger<SparseRowMatrixList> ();
        }

        public SparseRowMatrixList (int rowCount, int colCount, int initvalues) {
            ContractAssertions.Requires<ArgumentOutOfRangeException> (rowCount >= 0, "RowCount must be positive");
            ContractAssertions.Requires<ArgumentOutOfRangeException> (colCount >= 0, "colCount must be positive");
            ContractAssertions.Requires<ArgumentOutOfRangeException> (initvalues >= 0, "initvalues must be positive");

            this.rowCount = rowCount;
            this.colCount = colCount;
        }

        public int NonZeroValueCount {
            get { return _NonZeroValueCount; }
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
            if (val.ContainsKey (row)) {
                return (val[row]).Length;
            }
            ContractAssertions.Requires<IndexOutOfRangeException> (row >= 0 && row <= rowCount, "Array Index out of bounds");
            return 0;
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
            return rowLength (row);
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
            if (val.ContainsKey (row)) {
                return (val[row]).Length;
            }
            return 0;
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
            return rowLength (row);
        }

        public double this [int i, int j] {
            get { return this.At (i, j); }
            set { At (i, j, value); }
        }

        public double At (int i, int j) {
            ContractAssertions.Requires<ArgumentOutOfRangeException> (i >= 0 && i <= rowCount, "Array Index out of bounds");
            ContractAssertions.Requires<ArgumentOutOfRangeException> (j >= 0 && j <= colCount, "Array Index out of bounds");
            if (!val.ContainsKey (i)) return 0;
            int index = Array.BinarySearch (val[i], 0, rowLength (i), j);
            return (index < 0) ? 0.0 : val[i][index].value;
        }

        public void At (int i, int j, double value) {
            ContractAssertions.Requires<ArgumentOutOfRangeException> (i >= 0 && i <= rowCount, "Array Index out of bounds");
            ContractAssertions.Requires<ArgumentOutOfRangeException> (j >= 0 && j <= colCount, "Array Index out of bounds");

            if (!val.ContainsKey (i)) {
                val.Add (i, new SparseRowValue[1]);
                val[i][0].index = j;
                val[i][0].value = value;
                return;
            }

            int index = 0;
            index = Array.BinarySearch (val[i], 0, rowLength (i), j);
            //Console.WriteLine("after Binary search {0}", index);

            if (index < 0) {
                if (value == 0.0D) return;
                index = (index * -1) - 1;

                SparseRowValue[] s = val[i];
                Array.Resize (ref s, s.Length + 1);

                for (int t = s.Length - 1; t > index; t--) {
                    s[t] = s[t - 1];
                }
                //Console.WriteLine("After Val Move {0}-{1}--{2}", i, rowPtr.Length, rowCount);
                //Console.WriteLine("After RowPtr Move {0}", index);
                s[index].value = value;
                s[index].index = index;
                val[i] = s;
            } else {
                //  If Value is zero then we must delete the entry
                if (value == 0) {
                    Array.Copy (val[i], index + 1, val[i], index, val[i].Length - index);
                    SparseRowValue[] s = val[i];
                    Array.Resize (ref s, s.Length - 1);
                    val[i] = s;
                } else val[i][index].value = value;
            }

        }

        public void print () {
            for (int i = 0; i < rowCount; i++) {
                for (int j = 0; j < colCount; j++)
                    Console.Write (String.Format ("{0} ", At (i, j)));
                Console.WriteLine ();
            }
        }

        public SparseMatrix Transpose () {
            SparseRowMatrixList trans = new SparseRowMatrixList (colCount, rowCount);
            int[] rowCounts = new int[colCount];
            rowCounts.Initialize ();

            for (int i = 0; i < rowCount; i++) {
                int len = (val.ContainsKey (i)) ? val[i].Length : 0;
                for (int j = 0; j < len; j++)
                    rowCounts[val[i][j].index]++;
            }

            for (int i = 0; i < colCount; i++) {
                trans.val[i] = new SparseRowValue[rowCounts[i]];
                rowCounts[i] = 0;
            }

            for (int i = 0; i < rowCount; i++) {
                int len = (val.ContainsKey (i)) ? val[i].Length : 0;
                for (int j = 0; j < len; j++) {
                    int col = val[i][j].index;
                    trans.val[col][rowCounts[col]].index = i;
                    trans.val[col][rowCounts[col]].value = val[i][j].value;
                    rowCounts[col]++;
                }
            }

            return trans;
        }

        public double dot (int row, double[] s) {
            double ret = 0;

            foreach (SparseRowValue spr in val[row]) {
                ret += s[spr.index] * spr.value;
            }

            return (ret);
        }

        public void axpy (double a, int row, double[] y) {
            SparseRowValue[] rowArr = val[row];
            foreach (SparseRowValue spr in val[row]) {
                y[spr.index] = a * spr.value;
            }
        }

        public double nrm2_sq (int row) {
            double ret = 0;

            foreach (SparseRowValue spr in val[row]) {
                ret += spr.value * spr.value;
            }

            return (ret);
        }

        public IEnumerable<SparseRowValue> getRow (int row) {
            if (val.ContainsKey (row)) {
                SparseRowValue[] rowArr = val[row];
                for (int j = 0; j < rowArr.Length; j++)
                    yield return rowArr[j];
            }
        }

        public RowArrayPtr<SparseRowValue> getRowPtr (int row) {
            //Console.WriteLine("Creating row ptr {0} {1} ", row, val[row].Length-1);
            if (val.ContainsKey (row)) {
                return new RowArrayPtr<SparseRowValue> (val[row], 0, val[row].Length - 1);
            }
            return RowArrayPtr<SparseRowValue>.Empty ();
        }

        public void CopyRow (int toRow, SparseMatrix from, int fromRow) {
            SparseRowMatrixList.CopyRow (this, toRow, (SparseRowMatrixList) from, fromRow);
        }

        public static void CopyRow (SparseRowMatrixList to, int row, SparseRowMatrixList from, int fromRow) {

            int frl = from.rowLength (fromRow);

            if (frl == 0) return;

            //Console.WriteLine("Copyrow - "+ to.NonZeroValueCount+  " ," + to.val.Length +" ,"+ frl+" ,"+ (frl/ARRAYINCREMENT)  );
            if (!to.val.ContainsKey(row)) {
                to.val.Add(row, new SparseRowValue[from.val[fromRow].Length]);
            }
            else if (to.val[row].Length < from.val[fromRow].Length) {

                SparseRowValue[] rowArr = to.val[row];
                Array.Resize (ref rowArr, from.val[fromRow].Length);
                to.val[row] = rowArr;
            }

            Array.Copy (from.val[fromRow], to.val[row], from.val[fromRow].Length);
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

            //Console.WriteLine("here");

            if ((val.ContainsKey (row) && val[row].Length != 0) && _SequentialLoad == 0) throw new ArgumentException ("Row alreadys exists");
            // Console.WriteLine("here");
            SparseRowValue[] rowArr = null;
            if (val.ContainsKey (row)) rowArr = val[row];
            if (rowArr == null) {
                rowArr = new SparseRowValue[newRowLen];
            } else if (rowArr.Length < newRowLen) {

                Array.Resize (ref rowArr, newRowLen);
            }

            for (int j = 0; j < newRowLen; j++) {
                rowArr[j].index = cols[j];
                rowArr[j].value = vals[j];
            }
            val[row] = rowArr;
        }
    }

}