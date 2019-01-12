using System;
using Xunit;
using Xunit.Abstractions;
using liblinear;

/// <summary>
///
/// </summary>
public class ParameterTest {

    [Fact]
    public void ParameterCloneTest() {

        Parameter  p = new Parameter();

        p.eps = 0.1;
        p.C = 0.2;
        p.nr_weight = 3;
        p.weight_label = new int[3];
        p.weight = new double[3];
        p.p = 0.3;
        p.init_sol = new double[3];
        p.init_sol[0]=1.0;

        Parameter p2 = new Parameter(p);
        Assert.Equal(p.eps, p2.eps);
        p2.eps = 1.1;
        Assert.NotEqual(p.eps, p2.eps);

        Assert.Equal(p.init_sol[0], p2.init_sol[0]);
        p2.init_sol[0]=2.0;
        Assert.NotEqual(p.init_sol[0], p2.init_sol[0]);
    }
}