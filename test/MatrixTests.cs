using System;
using Xunit;
using Xunit.Abstractions;
using liblinear;
using System.Diagnostics;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class MatrixTests {


    [Fact]
    public void EmptyMatrixCreationAndCheck() {
        
        // CHeck Matrix Factory
        SparseMatrix mat = SparseMatrixFactory.CreateMatrix(10, 100);
        Assert.Equal(mat.RowCount(), 10);
        Assert.Equal(mat.ColCount(), 100);
        Assert.Equal(mat.At(0,0), 0.0D);
        Assert.Throws<ArgumentOutOfRangeException>(() => mat.At(11,11));
        Assert.Throws<ArgumentOutOfRangeException>(() => mat.At(-1, 11));
        Assert.Throws<ArgumentOutOfRangeException>(() => mat.At(0, 101));
        Assert.Throws<ArgumentOutOfRangeException>(() => mat.At(11, -1));

    }

    public class MatrixClassData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { new SparseRowMatrix(10,100) };
            yield return new object[] { new SparseRowMatrixVN(10,100) };
            yield return new object[] { new SparseRowMatrixList(10,100) };
            yield return new object[] { new SparseRowMatrixArr(10,100) };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Theory]
    [ClassData(typeof(MatrixClassData))]
    public void MatrixCheck(SparseMatrix sm) {
        Assert.Equal(sm.RowCount(), 10);
        Assert.Equal(sm.ColCount(), 100);
        Assert.Equal(sm.At(0,0), 0.0D);

        Assert.Throws<ArgumentOutOfRangeException>(() => sm.At(11,11));
        Assert.Throws<ArgumentOutOfRangeException>(() => sm.At(-1, 11));
        Assert.Throws<ArgumentOutOfRangeException>(() => sm.At(0, 101));
        Assert.Throws<ArgumentOutOfRangeException>(() => sm.At(11, -1));


        // Set Values and Retrieve
        sm.At(9,9, 10.0);
        Assert.Equal(10.0D, sm.At(9,9));
        Assert.Equal(0.0D, sm.At(9,10));

        // Transpose Checks
        sm.At(2,11, 11.0);
        SparseMatrix t = sm.Transpose();
        Assert.Equal(10.0D, t.At(9,9));
        Assert.Equal(0.0D, t.At(9,10));
        Assert.Equal(11.0D, t.At(11,2));
        Assert.Equal(10, t.ColCount());
        Assert.Equal(100, t.RowCount());

        // Calculations Check: dot, axpy 
        double[] w = new double[100];
        for(int i = 0; i < 10; i++) {
            sm.At(1, i, (double)i+1);
            w[i] = (double)10 - i;
        }

        w[10] = 40.0;

        // nrm2_sq
        Assert.Equal(385, sm.nrm2_sq(1) );

        // dot
        Assert.Equal(220, sm.dot(1, w));
        sm.At(1,10, 1);
        Assert.Equal(260, sm.dot(1, w));

        // axpy(double a, int row, double[] y)
        for(int i = 0; i < 100; i++)
            w[i]=0;
        
        sm.axpy(10, 1, w);
        Assert.Equal(30, w[2]);
        Assert.Equal(10, w[10]);
        Assert.Equal(0,  w[11]);


        // IEnumerable<SparseRowValue> getRow(int row);
        IEnumerable<SparseRowValue> vr = sm.getRow(1);
        int k = 0;
        foreach(SparseRowValue sr in vr)
            k++;
        Assert.Equal(11, k);

        vr = sm.getRow(7);
        k = 0;
        foreach(SparseRowValue sr in vr)
            k++;
        Assert.Equal(0, k);

        // public RowArrayPtr<SparseRowValue> getRowPtr(int row)
        RowArrayPtr<SparseRowValue> rowPtr = sm.getRowPtr(1);
        Assert.Equal(11, rowPtr.Length);
        rowPtr = sm.getRowPtr(7);
        Assert.Equal(0, rowPtr.Length);

        SparseMatrix cpSm = (SparseMatrix)Activator.CreateInstance(sm.GetType(), new object[] {sm.RowCount(), sm.ColCount()});
        cpSm.CopyRow(3, sm, 1);
        Assert.Equal(cpSm.RowCount(), 10);
        Assert.Equal(cpSm.ColCount(), 100);
        Assert.Equal(cpSm.At(0,0), 0.0D);
        rowPtr = cpSm.getRowPtr(3);
        Assert.Equal(11, rowPtr.Length);
        Assert.Equal(386, cpSm.nrm2_sq(3) );

        // TODO: LoadRow.



    }


    [Fact]
    public void SparseRowMatrixCheck() {
        SparseRowMatrix sm = new SparseRowMatrix(10,100); 
        Assert.Equal(0, sm.NonZeroValueCount);
        sm.At(8,8, 0);
        Assert.Equal( 0.0D, sm.At(8,8));  //  Should not insert a value
        Assert.Equal(0, sm.NonZeroValueCount);    

        Assert.Equal(sm.rowLength(0), 0);
        Assert.Throws<IndexOutOfRangeException>(() => sm.rowLength(-2));
        Assert.Throws<ArgumentOutOfRangeException>(() => sm.RowLengthSafe(-2));
        Assert.Throws<IndexOutOfRangeException>(() => sm.rowLength(101));
        Assert.Throws<ArgumentOutOfRangeException>(() => sm.RowLengthSafe(101));
    }


    // TODO:  Tests for other matrix classes for non-polymorphic functions.


    // TODO:  Tests for Matrix Facade
}
