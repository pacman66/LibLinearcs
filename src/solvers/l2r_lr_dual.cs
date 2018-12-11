using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace liblinearcs {
    public class l2r_lr_dual  {

        static RandomNumberGenerator rand = RandomNumberGenerator.Create(CONSTANTS.RANDOM_SEED);

        ILogger<l2r_lr_dual> _logger;

        public l2r_lr_dual() {
            _logger = ApplicationLogging.CreateLogger<l2r_lr_dual>();
        }

        // A coordinate descent algorithm for
        // the dual of L2-regularized logistic regression problems
        //
        //  min_\alpha  0.5(\alpha^T Q \alpha) + \sum \alpha_i log (\alpha_i) + (upper_bound_i - \alpha_i) log (upper_bound_i - \alpha_i),
        //    s.t.      0 <= \alpha_i <= upper_bound_i,
        //
        //  where Qij = yi yj xi^T xj and
        //  upper_bound_i = Cp if y_i = 1
        //  upper_bound_i = Cn if y_i = -1
        //
        // Given:
        // x, y, Cp, Cn
        // eps is the stopping tolerance
        //
        // solution will be put in w
        //
        // See Algorithm 5 of Yu et al., MLJ 2010
        // #undef GETI
        // #define GETI(i) (y[i]+1)
        // To support weights for instances, use GETI(i) (i)
        public void  solve(Problem prob, ref double[] w, double eps, double Cp, double Cn)
        {
            int l = prob.l;
            int w_size = prob.n;
            int i, s, iter = 0;
            double[] xTx = new double[l];
            int max_iter = 1000;
            int[] index = new int[l];
            double[] alpha = new double[2*l]; // store alpha and C - alpha
            sbyte[] y = new sbyte[l];
            int max_inner_iter = 100; // for inner Newton
            double innereps = CONSTANTS.INNER_EPS;
            double innereps_min = Math.Min(CONSTANTS.INNER_EPS_SML, eps);
            double[] upper_bound = {Cn, 0, Cp};

            for(i=0; i<l; i++)
            {
                y[i] = (sbyte)((prob.y[i] > 0)  ? +1 : -1);
            }

            // Initial alpha can be set here. Note that
            // 0 < alpha[i] < upper_bound[GETI(i)]
            // alpha[2*i] + alpha[2*i+1] = upper_bound[GETI(i)]
            for(i=0; i<l; i++)
            {
                alpha[2*i] = Math.Min(0.001*upper_bound[(y[i]+1)], CONSTANTS.INNER_EPS_SML); ////#define GETI(i) (y[i]+1)
                alpha[2*i+1] = upper_bound[(y[i]+1)] - alpha[2*i]; //#define GETI(i) (y[i]+1)
            }

            // for(i=0; i<w_size; i++)
            //     w[i] = 0;

            Array.Clear(w, 0, w_size);

            for(i=0; i<l; i++)
            {
                // feature_node[] xi = prob.x[i];
                // xTx[i] = sparse_operator.nrm2_sq(xi);
                // sparse_operator.axpy(y[i]*alpha[2*i], xi, w);
                xTx[i] = prob.x.nrm2_sq(i);
                prob.x.axpy(y[i]*alpha[2*i], i, w);
                index[i] = i;
            }

            while (iter < max_iter)
            {
                for (i=0; i<l; i++)
                {
                    int j = i+rand.Next(l-i);
                    Helper.swap<int>(ref index[i], ref index[j]);
                }
                int newton_iter = 0;
                double Gmax = 0;
                for (s=0; s<l; s++)
                {
                    i = index[s];
                    sbyte yi = y[i];
                    double C = upper_bound[(y[i]+1)]; //#define GETI(i) (y[i]+1)
                    double ywTx = 0, xisq = xTx[i];
                    // feature_node[] xi = prob.x[i];
                    // ywTx = ((double)yi)*sparse_operator.dot(w, xi);
                    ywTx = ((double)yi)*prob.x.dot(i, w);
                    double a = xisq, b = ywTx;

                    // Decide to minimize g_1(z) or g_2(z)
                    int ind1 = 2*i, ind2 = 2*i+1, sign = 1;
                    if(0.5*a*(alpha[ind2]-alpha[ind1])+b < 0)
                    {
                        ind1 = 2*i+1;
                        ind2 = 2*i;
                        sign = -1;
                    }

                    //  g_t(z) = z*log(z) + (C-z)*log(C-z) + 0.5a(z-alpha_old)^2 + sign*b(z-alpha_old)
                    double alpha_old = alpha[ind1];
                    double z = alpha_old;
                    if(C - z < 0.5 * C)
                        z = 0.1*z;
                    double gp = a*(z-alpha_old)+sign*b+Math.Log(z/(C-z));
                    Gmax = Math.Max(Gmax, Math.Abs(gp));

                    // Newton method on the sub-problem
                    const double eta = 0.1; // xi in the paper
                    int inner_iter = 0;
                    while (inner_iter <= max_inner_iter)
                    {
                        if(Math.Abs(gp) < innereps)
                            break;
                        double gpp = a + C/(C-z)/z;
                        double tmpz = z - gp/gpp;
                        if(tmpz <= 0)
                            z *= eta;
                        else // tmpz in (0, C)
                            z = tmpz;
                        gp = a*(z-alpha_old)+sign*b+Math.Log(z/(C-z));
                        newton_iter++;
                        inner_iter++;
                    }

                    if(inner_iter > 0) // update w
                    {
                        alpha[ind1] = z;
                        alpha[ind2] = C-z;
                        prob.x.axpy(sign*(z-alpha_old)*yi, i, w);
                    }
                }

                iter++;
                if(iter % 10 == 0)
                    _logger.LogInformation(".");

                if(Gmax < eps)
                    break;

                if(newton_iter <= l/10)
                    innereps = Math.Max(innereps_min, 0.1*innereps);

            }

            _logger.LogInformation("\noptimization finished, #iter = {0}\n",iter);
            if (iter >= max_iter)
                _logger.LogInformation("\nWARNING: reaching max number of iterations\nUsing -s 0 may be faster (also see FAQ)\n\n");

            // calculate objective value

            double v = 0;
            for(i=0; i<w_size; i++)
                v += w[i] * w[i];
            v *= 0.5;
            for(i=0; i<l; i++)
                v += alpha[2*i] * Math.Log(alpha[2*i]) + alpha[2*i+1] * Math.Log(alpha[2*i+1])
                    - upper_bound[(y[i]+1)] * Math.Log(upper_bound[(y[i]+1)]);  //#define GETI(i) (y[i]+1)
            _logger.LogInformation("Objective value = {0}\n", v);

            return ;
        }
    }
}

