using System;
using Xunit;
using Xunit.Abstractions;
using liblinearcs;

using System.Diagnostics;
using System.IO;

public class ModelTests {

   // private readonly ITestOutputHelper output;
    //private readonly IMessageSink diagnosticMessageSink;


    // public ModelTests(ITestOutputHelper output)//, IMessageSink diagnosticMessageSink)
    // {
    //     this.output = output;
    //     //this.diagnosticMessageSink = diagnosticMessageSink;
    //  }

    [Fact]
    public void TestSetters() {
        SparseMatrix m2 = SparseMatrixFactory.CreateMatrix(10, 100);

        Assert.Equal(m2.RowCount(), 10);
        
        Model m = new Model();

        m.nr_class = 10;
        m.nr_feature = 12;
        Assert.Equal(10, m.nr_class);
        Assert.Equal(12, m.nr_feature);

        Parameter p = new Parameter();
        p.eps = 0.01e-12;
        p.nr_weight = 10;
        p.solver_type = SOLVER_TYPE.L2R_LR_DUAL;
        p.init_sol=new double[100];
        p.init_sol[0] = 130.09229;
        p.p=0.123029;
        p.C=1.20299;
        
        m.param = p;

        Assert.Equal(p.eps, m.param.eps);
        Assert.Equal(p.C, m.param.C);
        Assert.Equal(p.init_sol[0], m.param.init_sol[0]);
        Assert.Equal(p.p, m.param.p);
        Assert.Equal(p.nr_weight, m.param.nr_weight);
        Assert.Equal(p.solver_type, m.param.solver_type);

        p.eps = 1309;
        p.init_sol[0]=999;
        Assert.NotEqual(p.eps, m.param.eps);
        Assert.NotEqual(p.init_sol[0], m.param.init_sol[0]);
        
        double[] w ={1.0, 2.0, 3.0};
        m.w = w;
        Assert.Equal(w[0], m.w[0]);
        m.w[0] = 5.0;
        Assert.NotEqual(w[0], m.w[0]);
        w[0] = 5.0;
        Assert.Equal(w[0], m.w[0]);

        int[] labels = {1,2,3,4,5,6};
        m.label = labels;
        Assert.Equal(labels[1],m.label[1]);
        m.label[1] = 9;
        Assert.NotEqual(labels[1],m.label[1]);
        labels[1]=9;
      
     
        Assert.Equal(labels[1],m.label[1]);

    }

    [Theory]
    [InlineData(SOLVER_TYPE.L2R_L2LOSS_SVR)]
    [InlineData(SOLVER_TYPE.L2R_L1LOSS_SVR_DUAL)]
    [InlineData(SOLVER_TYPE.L2R_L2LOSS_SVR_DUAL)]
    public void Testget_w_value_L2R_L2Loss(SOLVER_TYPE st) {
        Model m = new Model();
        Parameter p = new Parameter();
        double [] w = {1.0,2.0,3.0};
        int idx = 2;
        int label_idx = 2;
        p.solver_type = st;
        m.param = p;
        m.nr_class = 2;
        m.nr_feature=3;
        m.w=w;
        
        //      IF
        //          solver_type==SOLVER_TYPE.L2R_L2LOSS_SVR ||
        //            solver_type==SOLVER_TYPE.L2R_L1LOSS_SVR_DUAL ||
        //            solver_type==SOLVER_TYPE.L2R_L2LOSS_SVR_DUAL);  
        //       Shuld return w[idx] 
        double t = Model.get_w_value(m, idx, label_idx);
        Assert.Equal(w[2], t);

    }

    // TODO :  idx < 0 || idx > model_.nr_feature  => 0
    // TODO : W is null  ??
    // TODO : label_idx < 0 || label_idx >= nr_class => 0 
    // TODO : nr_class == 2 && solver_type != SOLVER_TYPE.MCSVM_CS)            {   if(label_idx == 0)  return w[idx];   else    return -w[idx]; }
    // TODO : NONE of the ABOVE =>  w[idx*nr_class+label_idx];    
}