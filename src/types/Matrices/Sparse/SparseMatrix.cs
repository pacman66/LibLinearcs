using System;
using System.Collections.Generic;

namespace liblinearcs {

    public interface SparseMatrix {
        int RowCount();
        int ColCount();
        double At(int i, int j);
        void At(int i, int j, double value);
        SparseMatrix Transpose();
        void print();
        double dot(int row, double[] s);
        void axpy(double a, int row, double[] y);
        double nrm2_sq(int row);

        IEnumerable<SparseRowValue> getRow(int row);

        RowArrayPtr<SparseRowValue> getRowPtr(int row);

        void CopyRow(int toRow, SparseMatrix from, int fromRow);

        void LoadRow(int row, int[] cols, double[] vals, int length);

        Boolean SequentialLoad {get; set;}
    }
}