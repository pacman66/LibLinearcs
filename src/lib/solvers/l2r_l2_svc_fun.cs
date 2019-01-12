using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace liblinear {
    public class l2r_l2_svc_fun : IFunction
    {
        protected double[] C;
        protected double[] z;
        protected int[] I;
        protected int sizeI;
        protected Problem prob;

        private ILogger<l2r_l2_svc_fun> _logger;

        public l2r_l2_svc_fun(Problem prob, double[] C)
        {
            this.prob = prob;
            this.z = new double[prob.l];
            this.I = new int[prob.l];
            this.C = C;
            _logger = ApplicationLogging.CreateLogger<l2r_l2_svc_fun>();

            
        }

        public double fun(double[] w) {
            int i;
            double f=0;
            double[] y=prob.y;
            int l=prob.l;
            int w_size=get_nr_variable();

            Xv(w, z);

            for(i = 0;i < w_size; i++)
                f += w[i] * w[i];

            f /= 2.0;

            for(i = 0; i < l; i++)
            {
                z[i] = y[i] * z[i];
                double d = 1 - z[i];
                if (d > 0)
                    f += C[i] * d * d;
            }

            return(f);
        }

        public void grad(double[] w, double[] g) {
            int i;
            double[] y=prob.y;
            int l=prob.l;
            int w_size=get_nr_variable();

            sizeI = 0;
            for (i = 0; i < l; i++)
                if (z[i] < 1)
                {
                    z[sizeI] = C[i] * y[i] * (z[i]-1);
                    I[sizeI] = i;
                    sizeI++;
                }

            subXTv(z, g);

            for(i=0;i<w_size;i++)
                g[i] = w[i] + 2 * g[i];   
        }
        public void Hv(double[] s, double[] Hs) {
            int i;
            int w_size = get_nr_variable();
            // feature_node[][] x=prob.x;
            SparseMatrix x = prob.x;

            for(i = 0; i < w_size; i++)
                Hs[i]=0;

            for(i = 0; i < sizeI; i++)
            {
                //feature_node[] xi=x[I[i]];

                double xTs = x.dot(I[i], s);
                //double xTs = sparse_operator.dot(s, xi);

                xTs = C[I[i]] * xTs;

                x.axpy(xTs, I[i], Hs);
                //sparse_operator.axpy(xTs, xi, Hs);
            }
            for(i = 0; i < w_size; i++)
                Hs[i] = s[i] + 2 * Hs[i];
        }

        public int get_nr_variable() {
            return prob.n;
        }
        public void get_diag_preconditioner(double[] M) {
            int i;
            int w_size=get_nr_variable();
            //feature_node[][] x = prob.x;
            SparseMatrix x = prob.x;

            for (i = 0; i < w_size; i++)
                M[i] = 1;

            for (i = 0; i < sizeI; i++)
            {
                int idx = I[i];
                
                IEnumerable<SparseRowValue> s =  x.getRow(idx);
                //feature_node[] s = x[idx];

                foreach(SparseRowValue vn in s) {
                    M[vn.index] += vn.value * vn.value * C[idx] * 2;
                }

                // while (s[k].index!=-1)
                // {
                //     M[s[k].index-1] += s[k].value*s[k].value*C[idx]*2;
                //     k++;
                // }
            }       
        }

        protected void Xv(double[] v, double [] Xv) {
            int l=prob.l;
            //feature_node[][] x=prob.x;
            SparseMatrix x = prob.x;

            for(int i = 0 ;i < l; i++)
                Xv[i] = x.dot(i, v);
//                Xv[i]=sparse_operator.dot(v, x[i]);
        }

        protected  void subXTv(double[] v, double[] XTv) {
            int i;
            int w_size=get_nr_variable();
            //feature_node[][] x=prob.x;
            SparseMatrix x = prob.x;

            for(i = 0; i < w_size; i++)
                XTv[i]=0;

            for(i = 0; i < sizeI; i++)
                x.axpy(v[i], I[i], XTv);
//                sparse_operator.axpy(v[i], x[I[i]], XTv);
        }

    }
}