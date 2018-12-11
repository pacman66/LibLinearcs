using System;
using System.Collections.Generic;

namespace liblinearcs {

    public class SparseMatrixFactory {

        public static SparseMatrix CreateMatrix(int rowCount, int colCount, int initvalues) {
            return (SparseMatrix)new SparseRowMatrixArr( rowCount,  colCount,  initvalues);
        }

        public static SparseMatrix CreateMatrix(int rowCount, int colCount) {
            return (SparseMatrix)new SparseRowMatrixArr( rowCount,  colCount);
        }

        
        public static SparseMatrix CreateFacadeMatrix(SparseMatrix matrix, int[] rowMappings) {
            return (SparseMatrix)new SparseMatrixPermFacade( matrix,  rowMappings);
        }

    }

}