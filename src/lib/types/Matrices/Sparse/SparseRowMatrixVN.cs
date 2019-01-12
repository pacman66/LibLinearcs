using System;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace liblinear {

    public class SparseRowMatrixVN : SparseMatrix
    {
        static readonly int ARRAYINCREMENT = 1000;

        int rowCount = 0;

        int colCount = 0;

        int[] rowPtr = null;

        SparseRowValue[] val = null;

        ILogger<SparseRowMatrixVN> _logger;

        public SparseRowMatrixVN(int rowCount, int colCount){

            ContractAssertions.Requires<ArgumentOutOfRangeException>( rowCount >= 0,  "RowCount must be positive");
            ContractAssertions.Requires<ArgumentOutOfRangeException>( rowCount >= 0,  "colCount must be positive");

            this.rowCount = rowCount;
            rowPtr = new int[rowCount+1];
            this.colCount = colCount;
            val = new SparseRowValue[0];
            _logger = ApplicationLogging.CreateLogger<SparseRowMatrixVN>();
        }

        public SparseRowMatrixVN(int rowCount, int colCount, int initvalues) {

            ContractAssertions.Requires<ArgumentOutOfRangeException>( rowCount >= 0,  "RowCount must be positive");
            ContractAssertions.Requires<ArgumentOutOfRangeException>( colCount >= 0,  "colCount must be positive");
            ContractAssertions.Requires<ArgumentOutOfRangeException>( initvalues >= 0,  "initvalues must be positive");

            this.rowCount = rowCount;
            rowPtr = new int[rowCount+1];
            Array.Clear(rowPtr, 0 , rowCount + 1 );
            this.colCount = colCount;
            val = new SparseRowValue[initvalues];
        }

        // public struct SparseRowValue : IComparable {
        //     public int index;

        //     public double value;

        //     public SparseRowValue(int index, double value) {
        //         this.value = value;
        //         this.index = index;
        //     }

        //     public void set(int index, double value) {
        //         this.value = value;
        //         this.index = index;
        //     }

        //     public int CompareTo(Object node) {
        //        // Console.WriteLine("Comparing {0} and {1}", this.index, node);
        //         return Math.Sign(this.index - (int)node);
        //     }
        // }

        public int NonZeroValueCount {
            get { return rowPtr[rowCount]; }
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int rowLength(int row) {
            return rowPtr[row+1] - rowPtr[row];
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
            return rowPtr[row+1] - 1;
        }


        /// <summary>
        /// The length (number of values) in the row
        /// </summary>
        /// <param name="row">The row</param>
        /// <returns>The requested length of the row.</returns>
        /// <remarks>range-checked.</remarks>
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int RowLengthSafe(int row) {
            ContractAssertions.Requires<ArgumentOutOfRangeException>( row >= 0 && row <= rowCount, "Array Index out of bounds");
            return rowPtr[row+1] - rowPtr[row];
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
            return rowPtr[row+1] - 1;
        }


        public double this[int i, int j] {
            get { return this.At(i, j); }
            set { At(i, j, value); }
        }

        public double At(int i, int j) {
            ContractAssertions.Requires<ArgumentOutOfRangeException>( i >= 0 && i <= rowCount, "Array Index out of bounds");
            ContractAssertions.Requires<ArgumentOutOfRangeException>( j >= 0 && j <= colCount, "Array Index out of bounds");
            int index = Array.BinarySearch(val, rowPtr[i], rowLength(i), j);
            return (index < 0) ? 0.0 : val[index].value;
        }

        public void At(int i, int j, double value) {
            ContractAssertions.Requires<ArgumentOutOfRangeException>( i >= 0 && i <= rowCount, "Array Index out of bounds");
            ContractAssertions.Requires<ArgumentOutOfRangeException>( j >= 0 && j <= colCount, "Array Index out of bounds");
            int index=0;
            index = Array.BinarySearch(val, rowPtr[i], rowLength(i), j);

            if (index < 0) {
                // TODO:  Need to insert value here
                // for efficiency should probably have a smaller array and merge periodically rather than all the time
                if (value==0.0) return;
                if ( NonZeroValueCount == val.Length) {
                    // Need to resize the array as hit maximum size
                    Array.Resize(ref val, val.Length + ARRAYINCREMENT);
                }
                index = (index * -1 ) - 1;

                for(int t = rowPtr[rowCount]; t > index; t--)
                    { 
                        val[t] = val[t-1];
                    }
                for (int t = i+1; t<rowCount+1; t++)
                    rowPtr[t]++;
                
                val[index] = new SparseRowValue(j, value);
            }
            else {
                //  If Value is zero then we must delete the entry
                if (value == 0) {
                    for(int t = i+1; t < rowCount+1; t++)
                        rowPtr[t]--;
                    
                    Array.Copy(val, index+1, val, index, rowPtr[rowCount]-index);
                }
                else val[index].value = value;
            }

        }

        // static int Closest(SparseRowValue[] arr, int start, int length, int col)  {
        //     //Console.WriteLine(String.Format("start {0}, length {1}, col {0}", start, length, col));
        //     if (length <= 0) return start;
            
        //     int left = start;
        //     int right = start + length-1;
        //     int mid = (right - left) / 2;

        //     if (col < arr[left].index) return left;
        //     if (col > arr[right].index) return right + 1; 
            
        //     if (arr[mid].index > col) return Closest(arr, left + 1, mid - 1, col);
        //     return Closest(arr, mid + 1, right - 1, col);

        // }

        public void print() {
            for(int i = 0; i < rowCount; i++) {
                for (int j=0; j<colCount; j++)
                    Console.Write(String.Format("{0} ", At(i,j)));
            Console.WriteLine();
            }
        }

        public SparseMatrix Transpose() {
            SparseRowMatrixVN trans = new SparseRowMatrixVN(colCount, rowCount);
            int[] rowCounts = new int[colCount];
            rowCounts.Initialize();
            
            for(int i = 0; i < NonZeroValueCount; i++)
                rowCounts[val[i].index]++;

            int y = 0;
            int t = 0;
            for(int i = 0; i < colCount; i++) {
                    trans.rowPtr[i] = y;
                    t = y;
                    y += rowCounts[i];                    
                    rowCounts[i] = t;
            }
             trans.rowPtr[colCount]=y;

            trans.val = new SparseRowValue[y];
            for(int i = 0; i < rowCount; i++) {
                for(int j = rowPtr[i]; j < rowPtr[i+1]; j++) {
                    trans.val[rowCounts[val[j].index]++].set(i, val[j].value);
                }
            }

            return trans;
        }


        public double dot(int row, double[] s) {
            double ret = 0;
            int end = rowPtr[row+1];
            for(int i = rowPtr[row]; i < end; i++ )
                ret += s[val[i].index] * val[i].value;

            return (ret);
        }

        public void axpy(double a, int row, double[] y)
        {
            int end = rowPtr[row+1];
            for(int i = rowPtr[row]; i < end; i++)
                y[val[i].index] += a * val[i].value;
        }


        public double nrm2_sq(int row)
        {
            double ret = 0;

            int end = rowPtr[row+1];
            for(int i = rowPtr[row]; i < end; i++) 
                 ret += val[i].value * val[i].value;

            return (ret);
        }

        public IEnumerable<SparseRowValue> getRow(int row) {
            for(int i = rowPtr[row]; i < rowPtr[row+1]; i++)
                yield return val[i];
        }


        public RowArrayPtr<SparseRowValue> getRowPtr(int row) {
            if (rowLength(row)>0)        return new RowArrayPtr<SparseRowValue>(val, rowPtr[row], rowPtr[row+1]-1);
            else return RowArrayPtr<SparseRowValue>.Empty();
        }


        public void CopyRow(int toRow, SparseMatrix from, int fromRow) {
            SparseRowMatrixVN.CopyRow(this, toRow, (SparseRowMatrixVN)from, fromRow);
        }


        public static void CopyRow(SparseRowMatrixVN to, int row, SparseRowMatrixVN from, int fromRow) {
            
            int frl = from.rowLength(fromRow);

            if (frl == 0) return;

            //Console.WriteLine("Copyrow - "+ to.NonZeroValueCount+  " ," + to.val.Length +" ,"+ frl+" ,"+ (frl/ARRAYINCREMENT)  );

            if ( to.NonZeroValueCount+frl > to.val.Length) {
                    // Need to resize the array as hit maximum size
                    Array.Resize(ref to.val, to.val.Length + (frl>ARRAYINCREMENT ? ((frl/ARRAYINCREMENT)+1)*ARRAYINCREMENT : ARRAYINCREMENT));
            }
 
            if (to.rowPtr[row]==to.NonZeroValueCount) {
                // points to the end of the array so can just append all the rows
                int trow = to.rowPtr[row];
                IEnumerable<SparseRowValue> f =  from.getRow(fromRow);

                foreach(SparseRowValue fi in f) {
                    to.val[trow++] = fi;
                }
                
                for(int i = row+1; i <= to.rowCount; i++)
                    to.rowPtr[i]=trow;
            } else {
                // Need to insert frl rows
                int trow = to.rowPtr[row];
                for(int t = to.rowPtr[to.rowCount]; t > trow; t--)
                    {                         
                        to.val[t] = to.val[t-1];
                    }

                IEnumerable<SparseRowValue> f =  from.getRow(fromRow);

                foreach( SparseRowValue fi in f) {
                    to.val[trow++] = fi;
                }

                for(int i = row+1; i <= to.rowCount; i++)
                    to.rowPtr[i] += frl;
            }
        }

        public Boolean checkValidity() {

            _logger.LogInformation("Check Non-zero Value Count = " + NonZeroValueCount);

            // Check all row lengths >=0 and row ptrs are pointing to values within val or at the end.
            for(int i=0; i < rowCount; i++) {
                    if (rowLength(i) < 0) {
                        Console.WriteLine("Negative Row Length row =" + i);
                        return false;
                    }
            }
            _logger.LogInformation("Check Row Lengths Completed sucessfully");


            for(int i = 0; i <  NonZeroValueCount; i++) {
                if (val[i].index <0) {
                    _logger.LogInformation("Negative Value Index val["+i+"] =" + val[i].index);
                    return false;
                }

                if (val[i].index >= colCount) {
                    _logger.LogInformation("Val Index too large val["+i+"] =" + val[i].index + " > " + colCount);
                    return false;
                }
            }
            _logger.LogInformation("All Checks Completed Sucessfully");
            return true;
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

            if (rowPtr[row] != rowPtr[row+1] && _SequentialLoad==0) throw new ArgumentException("Row alreadys exists");

        
            //Console.WriteLine("Copyrow - "+ to.NonZeroValueCount+  " ," + to.val.Length +" ,"+ frl+" ,"+ (frl/ARRAYINCREMENT)  );

            if ( NonZeroValueCount+newRowLen > val.Length) {
                    // Need to resize the array as hit maximum size
                    Array.Resize(ref val, val.Length + (newRowLen>ARRAYINCREMENT ? ((newRowLen/ARRAYINCREMENT)+1)*ARRAYINCREMENT : ARRAYINCREMENT));
            }
  
            // points to the end of the array so can just append all the rows
            int trow = rowPtr[row];
            
            for(int i = 0; i < newRowLen; i++) {
                val[trow].index=cols[i];
                val[trow].value= vals[i];
                trow++;
                }
                
            if (SequentialLoad) {
                    rowPtr[row+1]= trow;
            } else {
                    for(int i = row+1; i <= rowCount; i++)
                        rowPtr[i]=trow;
            }
        }
    }
}