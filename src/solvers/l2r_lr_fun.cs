using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace liblinearcs {
    class l2r_lr_fun : IFunction
    {

    readonly double[] C;
    readonly double[] z;
    readonly double[] D;
    readonly Problem prob;
    private ILogger<l2r_lr_fun> _logger;    

    public l2r_lr_fun(Problem prob, double[] C) {
        int l=prob.l;

        this.prob = prob;

        z = new double[l];
        D = new double[l];
        this.C = C;
        _logger = ApplicationLogging.CreateLogger<l2r_lr_fun>();
    }

    public double fun(double[] w)
    {
        int i;
        double f=0;
        double[] y=prob.y;
        int l=prob.l;
        int w_size=get_nr_variable();

        Xv(w, z);

        for(i=0;i<w_size;i++)
            f += w[i]*w[i];
        f /= 2.0;
        for(i=0;i<l;i++)
        {
            double yz = y[i]*z[i];
            if (yz >= 0)
                f += C[i]*Math.Log(1 + Math.Exp(-yz));
            else
                f += C[i]*(-yz+Math.Log(1 + Math.Exp(yz)));
        }

        return(f);
    }

    public void grad(double[] w, double[] g)
    {
        int i;
        double[] y=prob.y;
        int l=prob.l;
        int w_size=get_nr_variable();

        for(i=0;i<l;i++)
        {
            z[i] = 1/(1 + Math.Exp(-y[i]*z[i]));
            D[i] = z[i]*(1-z[i]);
            z[i] = C[i]*(z[i]-1)*y[i];
        }
        XTv(z, g);

        for(i=0;i<w_size;i++)
            g[i] = w[i] + g[i];
    }

    public int get_nr_variable()
    {
        return prob.n;
    }

    public void get_diag_preconditioner(double[] M)
    {
        int i;
        int l = prob.l;
        int w_size=get_nr_variable();
        //feature_node[][] x = prob.x;
        SparseMatrix x = prob.x;

        for (i=0; i<w_size; i++)
            M[i] = 1;

        for (i=0; i<l; i++)
        {
            // feature_node[] s = x[i];
            // int k = 0;
            // while (s[k].index!=-1)
            // {
            //     M[s[k].index-1] += s[k].value*s[k].value*C[i]*D[i];
            //     k++;
            // }

            IEnumerable<SparseRowValue> s =  x.getRow(i);

            foreach(SparseRowValue vn in s) {
                    M[vn.index] += vn.value*vn.value*C[i]*D[i];
              }

        }
    }

    public void Hv(double[] s, double[] Hs)
    {
        int i;
        int l=prob.l;
        int w_size=get_nr_variable();
        //feature_node[][]x=prob.x;
        SparseMatrix x = prob.x;

        for(i=0;i<w_size;i++)
            Hs[i] = 0;
        for(i=0;i<l;i++)
        {
            // feature_node[] xi=x[i];
            // double xTs = sparse_operator.dot(s, xi);
            double xTs =x.dot(i, s);

            xTs = C[i]*D[i]*xTs;

            x.axpy(xTs, i, Hs);
            //sparse_operator.axpy(xTs, xi, Hs);
        }
        for(i=0;i<w_size;i++)
            Hs[i] = s[i] + Hs[i];
    }

    private void Xv(double[] v, double[] Xv)
    {
        int i;
        int l=prob.l;
        //feature_node[][] x=prob.x;
        SparseMatrix x = prob.x;

        for(i=0;i<l;i++)
            Xv[i]=x.dot(i, v);
        //    Xv[i]=sparse_operator.dot(v, x[i]);
    }

    private void XTv(double[] v, double[] XTv)
    {
        int i;
        int l=prob.l;
        int w_size=get_nr_variable();
        //feature_node[][] x=prob.x;
        SparseMatrix x = prob.x;

        for(i=0;i<w_size;i++)
            XTv[i]=0;
        for(i=0;i<l;i++)
            x.axpy(v[i], i, XTv);
        //    sparse_operator.axpy(v[i], x[i], XTv);
    }

    };


     
}