using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using Microsoft.Extensions.Logging;

namespace liblinearcs {
    public class SparseMatrixPermFacade : SparseMatrix {

        int[] rowMappings = null;
        int rowCount = 0;
        SparseMatrix readOnlyMatrix;

        ILogger<SparseMatrixPermFacade> _logger;

        public SparseMatrixPermFacade(SparseMatrix matrix, int[] rowMappings) {
            this.rowCount = rowMappings.Length;
            this.rowMappings = rowMappings;
            readOnlyMatrix = matrix;
            _logger = ApplicationLogging.CreateLogger<SparseMatrixPermFacade>();
        }

        public int NonZeroValueCount {
            get { throw new NotImplementedException("Read Only copy, CopyRow Not Implemented"); //return rowPtr[rowCount];
             }
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
            return readOnlyMatrix.ColCount();
        }

        /// <summary>
        /// The length (number of values) in the row
        /// </summary>
        /// <param name="row">The row</param>
        /// <returns>The requested length of the row.</returns>
        /// <remarks>Not range-checked.</remarks>
    
        public double this[int i, int j] {
            get { return this.At(i, j); }
            set { At(i, j, value); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double At(int i, int j) {
            return readOnlyMatrix.At(rowMappings[i], j);
        }

        public void At(int i, int j, double value) {
            throw new NotImplementedException("Read Only copy, CopyRow Not Implemented");
            //readOnlyMatrix.At(rowMappings[i], j, value);
        }

        public void print() {
            for(int i = 0; i < rowCount; i++) {
                for (int j=0; j<readOnlyMatrix.ColCount(); j++)
                    Console.Write(String.Format("{0} ", readOnlyMatrix.At(rowMappings[i],j)));
            Console.WriteLine();
            }
        }

       public SparseMatrix Transpose() {
            throw new NotImplementedException("Read Only copy, CopyRow Not Implemented");
          // return null;
            // int colCount = readOnlyMatrix.ColCount();
            // Matrix trans = new SparseRowMatrix(colCount, rowCount);
            // int[] rowCounts = new int[colCount];
            // rowCounts.Initialize();
            
            // for(int i = 0; i < readOnlyMatrix.NonZeroValueCount; i++)
            //     rowCounts[colPtr[i]]++;

            // int y = 0;
            // int t = 0;
            // for(int i = 0; i < colCount; i++) {
            //         trans.rowPtr[i] = y;
            //         t = y;
            //         y += rowCounts[i];                    
            //         rowCounts[i] = t;
            // }
            //  trans.rowPtr[colCount]=y;

            // trans.val = new double[y];
            // trans.colPtr = new int[y];
            
            // for(int i = 0; i < rowCount; i++) {
            //     for(int j = rowPtr[i]; j < rowPtr[i+1]; j++) {
            //         trans.colPtr[rowCounts[colPtr[j]]] = i;
            //         trans.val[rowCounts[colPtr[j]]++] = val[j];
            //     }
            // }

            // return trans;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double dot(int row, double[] s) {
            return readOnlyMatrix.dot(rowMappings[row], s);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void axpy(double a, int row, double[] y)
        {
            readOnlyMatrix.axpy(a, rowMappings[row], y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double nrm2_sq(int row)
        {
            return readOnlyMatrix.nrm2_sq(rowMappings[row]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<SparseRowValue> getRow(int row) {
            return readOnlyMatrix.getRow(rowMappings[row]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RowArrayPtr<SparseRowValue> getRowPtr(int row) {
            return readOnlyMatrix.getRowPtr(rowMappings[row]);
        }

        public void LoadRow(int row, int[] cols, double[] vals, int length) {
            throw new NotImplementedException("Read Only copy, CopyRow Not Implemented");
        }

        public void CopyRow(int toRow, SparseMatrix from, int fromRow) {
              throw new NotImplementedException("Read Only copy, CopyRow Not Implemented");
        }


        public static void CopyRow(SparseRowMatrix to, int row, SparseRowMatrix from, int fromRow) {
            throw new NotImplementedException("Read Only copy, CopyRow Not Implemented");
        }
        
        public Boolean SequentialLoad
        {
            get {return (readOnlyMatrix.SequentialLoad);}
            set {readOnlyMatrix.SequentialLoad = value;}
        }
    }
}