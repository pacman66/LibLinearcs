using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace liblinear {
    public class l2r_l2_svr_fun : l2r_l2_svc_fun {
        private	double p;

        ILogger<l2r_l2_svr_fun> _logger;

        public l2r_l2_svr_fun(Problem prob, double[] C, double p) : base(prob, C) {
            this.p = p;

            _logger = ApplicationLogging.CreateLogger<l2r_l2_svr_fun>();
        }

	    public new double fun(double[] w) {
            int i;
            double f=0;
            double[] y=prob.y;
            int l=prob.l;
            int w_size=get_nr_variable();
            double d;

            Xv(w, z);

            for(i=0;i<w_size;i++)
                f += w[i]*w[i];
            f /= 2;
            for(i=0;i<l;i++)
            {
                d = z[i] - y[i];
                if(d < -p)
                    f += C[i]*(d+p)*(d+p);
                else if(d > p)
                    f += C[i]*(d-p)*(d-p);
            }

            return(f);
        }
	    public new void grad(double[] w, double[] g) {
            int i;
            double[] y=prob.y;
            int l=prob.l;
            int w_size=get_nr_variable();
            double d;

            sizeI = 0;
            for(i=0;i<l;i++)
            {
                d = z[i] - y[i];

                // generate index set I
                if(d < -p)
                {
                    z[sizeI] = C[i]*(d+p);
                    I[sizeI] = i;
                    sizeI++;
                }
                else if(d > p)
                {
                    z[sizeI] = C[i]*(d-p);
                    I[sizeI] = i;
                    sizeI++;
                }

            }
            subXTv(z, g);

            for(i=0;i<w_size;i++)
                g[i] = w[i] + 2*g[i];
        }


    }
}