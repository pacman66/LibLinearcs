
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using Microsoft.Extensions.Logging;

namespace liblinear {

    public class SparseRowMatrixArr : SparseMatrix {
        SparseRowValue[][] val = null;
        int rowCount = 0;
        int colCount = 0;

        int _NonZeroValueCount = 0;
        ILogger<SparseRowMatrixArr> _logger;

        public SparseRowMatrixArr(int rowCount, int colCount) {
            ContractAssertions.Requires<ArgumentOutOfRangeException>( rowCount >= 0,  "RowCount must be positive");
            ContractAssertions.Requires<ArgumentOutOfRangeException>( colCount >= 0,  "colCount must be positive");
            this.rowCount = rowCount;
            this.colCount = colCount;
            val = new SparseRowValue[rowCount][];

            _logger = ApplicationLogging.CreateLogger<SparseRowMatrixArr>();
        }


        public SparseRowMatrixArr(int rowCount, int colCount, int initvalues) {
            ContractAssertions.Requires<ArgumentOutOfRangeException>( rowCount >= 0,  "RowCount must be positive");
            ContractAssertions.Requires<ArgumentOutOfRangeException>( colCount >= 0,  "colCount must be positive");
            ContractAssertions.Requires<ArgumentOutOfRangeException>( initvalues >= 0,  "initvalues must be positive");

            this.rowCount = rowCount;
            this.colCount = colCount;
            val = new SparseRowValue[rowCount][];
        }

        public int NonZeroValueCount {
            get {  return _NonZeroValueCount;  }
        }

        /// <summary>
        /// Number of rows in the Matrix
        /// </summary>
        /// <returns>The number of rows in the Matrix</returns>
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public int RowCount() {
            return rowCount;
        }

        /// <summary>
        /// Number of Columns in the Matrix
        /// </summary>
        /// <returns>The number of Columns in the Matrix</returns>
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public int ColCount() {
            return colCount;
        }

        /// <summary>
        /// The length (number of values) in the row
        /// </summary>
        /// <param name="row">The row</param>
        /// <returns>The requested length of the row.</returns>
        /// <remarks>Not range-checked.</remarks>
        [ReliabilityContract(Consistency.MayCorruptProcess, Cer.MayFail)]
        public int rowLength(int row) {

            return (val[row]!=null) ? val[row].Length : 0;
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
        [ReliabilityContract(Consistency.MayCorruptProcess, Cer.MayFail)]
        public int RowEnd(int row) {
            return val[row].Length;
        }


        /// <summary>
        /// The length (number of values) in the row
        /// </summary>
        /// <param name="row">The row</param>
        /// <returns>The requested length of the row.</returns>
        /// <remarks>range-checked.</remarks>
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public int RowLengthSafe(int row) {
            ContractAssertions.Requires<ArgumentOutOfRangeException>( row >= 0 && row <= rowCount, "Array Index out of bounds");
            return  val[row].Length;
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
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public int rowEndSafe(int row) {
            ContractAssertions.Requires<ArgumentOutOfRangeException>( row >= 0 && row <= rowCount, "Array Index out of bounds");
            return  val[row].Length;;
        }

        public double this[int i, int j] {
            get { return this.At(i, j); }
            set { At(i, j, value); }
        }

        public double At(int i, int j) {
            ContractAssertions.Requires<ArgumentOutOfRangeException>( i >= 0 && i <= rowCount, "Array Index out of bounds");
            ContractAssertions.Requires<ArgumentOutOfRangeException>( j >= 0 && j <= colCount, "Array Index out of bounds");
            if (val[i]==null) return 0.0;
            int index = Array.BinarySearch(val[i], 0, rowLength(i), j);
            return (index < 0) ? 0.0 : val[i][index].value;
        }

        public void At(int i, int j, double value) {
            ContractAssertions.Requires<ArgumentOutOfRangeException>( i >= 0 && i <= rowCount, "Array Index out of bounds");
            ContractAssertions.Requires<ArgumentOutOfRangeException>( j >= 0 && j <= colCount, "Array Index out of bounds");
            int index=0;

            if ( val[i] == null) {
                val[i] = new SparseRowValue[1];
                val[i][0].value = value;
                val[i][0].index = j;
                return;
            }

            index = Array.BinarySearch(val[i], 0, rowLength(i), j);
       
            if (index < 0) {
                if (value == 0.0d) return;

                index = (index * -1 ) - 1;

                Array.Resize(ref val[i], val[i].Length+1);

                
                // Array.Copy(val[i], index + 1, val[i], index + 2, (val[i].Length - index)+1);

                // Array.Resize(ref s, s.Length+1);

                for(int t = val[i].Length-1; t > index; t--)
                    { 
                        val[i][t]=val[i][t-1];
                    }


                val[i][index].value=value;
                val[i][index].index=index;
                _NonZeroValueCount++;
            }
            else {
                //  If Value is zero then we must delete the entry
                if (value == 0) {
                    Array.Copy(val[i], index+1, val[i], index, val[i].Length-index);
                    Array.Resize(ref val[i], val[i].Length - 1);
                    _NonZeroValueCount--;
                }
                else val[i][index].value = value;
            }

        }


        public void print() {
            for(int i = 0; i < rowCount; i++) {
                for (int j=0; j<colCount; j++)
                    Console.Write(String.Format("{0} ", At(i,j)));
            Console.WriteLine();
            }
        }

       public SparseMatrix Transpose() {
            SparseRowMatrixArr trans = new SparseRowMatrixArr(colCount, rowCount);
            int[] rowCounts = new int[colCount];
            rowCounts.Initialize();
            
            for(int i = 0; i < rowCount; i++) {
                int len = (val[i] == null) ? 0 : val[i].Length;
                for (int j = 0 ; j < len; j++)
                    rowCounts[val[i][j].index]++;
            }

            for(int i = 0; i < colCount; i++) {
                    trans.val[i] = new SparseRowValue[rowCounts[i]];
                    rowCounts[i]=0;
            }
          
            for(int i = 0; i < val.Length; i++) {
                int len = (val[i] == null) ? 0 : val[i].Length;
                for (int j = 0 ; j < len; j++)
                {
                    int col = val[i][j].index;
                    trans.val[col][rowCounts[col]].index = i;
                    trans.val[col][rowCounts[col]].value = val[i][j].value;
                    rowCounts[col]++;
                }
            }

            return trans;
        }


        public double dot(int row, double[] s) {
            double ret = 0;

            SparseRowValue[] vR = val[row];
            for(int j = 0; j < vR.Length; j++) {
                ret += s[vR[j].index] * vR[j].value;
            }

            //ret = vR.AsParallel().Sum(index  => { return s[index.index] * index.value; });
            return (ret);
        }

        public void axpy(double a, int row, double[] y)
        {

            SparseRowValue[] vR = val[row];
            for(int j = 0; j < vR.Length; j++) {
                y[vR[j].index] += a * vR[j].value;
            }

            //vR.AsParallel().ForAll(index  => {  y[index.index]  += a * index.value; });
        }


        public double nrm2_sq(int row)
        {
            double ret = 0;

            SparseRowValue[] vR = val[row];
            for(int j = 0; j < vR.Length; j++) {
                 ret += vR[j].value * vR[j].value;
            }
//           ret = vR.AsParallel().Sum(index  => { return index.value * index.value; });
            return (ret);
        }

        
        public IEnumerable<SparseRowValue> getRow(int row) {
            if (val[row]!=null ) {
                for(int j = 0; j < val[row].Length; j++)
                     yield return val[row][j];
            }
        }

        public RowArrayPtr<SparseRowValue> getRowPtr(int row) {
            if (val[row] != null) return new RowArrayPtr<SparseRowValue>(val[row], 0, val[row].Length-1);
            else return RowArrayPtr<SparseRowValue>.Empty();
        }


        public void CopyRow(int toRow, SparseMatrix from, int fromRow) {
             SparseRowMatrixArr.CopyRow(this, toRow, (SparseRowMatrixArr)from, fromRow);
        }


        public static void CopyRow(SparseRowMatrixArr to, int row, SparseRowMatrixArr from, int fromRow) {
            
            int frl = from.rowLength(fromRow);

            if (frl == 0) return;

            if (to.val[row]==null) {
                to.val[row] = new SparseRowValue[frl];
            }
            else if (to.val[row].Length != frl)
                {
                    Array.Resize(ref to.val[row], frl);
                }
 
            Array.Copy(from.val[fromRow], to.val[row], frl);
        }

        private int _SequentialLoad = 0;
        
        public Boolean SequentialLoad
        {
            get {return (_SequentialLoad >0);}
            set {_SequentialLoad += (value) ? +1 : -1;}
        }

        public void LoadRow(int row, int[] cols, double[] vals, int newRowLen) {
            
            bool checkSorted = true;
            
            for(int i =1; i < newRowLen; i++)
                if (cols[i-1]>cols[i]) checkSorted = false;
            
            if (!checkSorted) throw new ArgumentException("Cols is not sorted");

            //Console.WriteLine("here");

            if ((val[row] != null && val[row].Length != 0) && _SequentialLoad==0) throw new ArgumentException("Row alreadys exists");
           // Console.WriteLine("here");

            if (val[row] == null) {
                val[row] = new SparseRowValue[newRowLen];
            }
            else  if (val[row].Length != newRowLen)
                {
                    Array.Resize(ref val[row], newRowLen);
                }
 
            _NonZeroValueCount += newRowLen;
            for(int j =0; j < newRowLen; j++) {
                val[row][j].index = cols[j];
                val[row][j].value = vals[j];
            }
        }
    }

}