using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace liblinear {
    public class l2r_l1l2_svr  {

        static RandomNumberGenerator rand = RandomNumberGenerator.Create(CONSTANTS.RANDOM_SEED);

        ILogger<l2r_l1l2_svr> _logger;

        public l2r_l1l2_svr() {
            _logger = ApplicationLogging.CreateLogger<l2r_l1l2_svr>();
        }


        public double[] solve(Problem prob, double[] w, Parameter param, SOLVER_TYPE solver_type)
        {
            int l = prob.l;
            double C = param.C;
            double p = param.p;
            int w_size = prob.n;
            double eps = param.eps;
            int i, s, iter = 0;
            int max_iter = 1000;
            int active_size = l;
            int[] index = new int[l];

            double d, G, H;
            double Gmax_old = double.PositiveInfinity;
            double Gmax_new, Gnorm1_new;
            double Gnorm1_init = -1.0; // Gnorm1_init is initialized at the first iteration
            double[] beta = new double[l];
            double[] QD = new double[l];
            double[] y = prob.y;

            // L2R_L2LOSS_SVR_DUAL
            double[] lambda = {0.5 / C };
            double[] upper_bound = { double.PositiveInfinity };

            if(solver_type == SOLVER_TYPE.L2R_L1LOSS_SVR_DUAL)
            {
                lambda[0] = 0;
                upper_bound[0] = C;
            }

            // Initial beta can be set here. Note that
            // -upper_bound <= beta[i] <= upper_bound
            // for(i=0; i<l; i++)
            //     beta[i] = 0;
            Array.Clear(beta, 0, l );

            // for(i=0; i<w_size; i++)
            //     w[i] = 0;
            Array.Clear(w, 0, w_size);

            for(i=0; i<l; i++)
            {
                // feature_node[] xi = prob.x[i];
                // QD[i] = sparse_operator.nrm2_sq(xi);
                // sparse_operator.axpy(beta[i], xi, w);
                QD[i] = prob.x.nrm2_sq(i);
                prob.x.axpy(beta[i], i, w);  
                index[i] = i;
            }


            while(iter < max_iter)
            {
                Gmax_new = 0;
                Gnorm1_new = 0;

                for(i=0; i<active_size; i++)
                {
                    int j = i+rand.Next(active_size-i);
                    Helper.swap<int>(ref index[i], ref index[j]);
                }

                for(s=0; s<active_size; s++)
                {
                    i = index[s];
                    G = -y[i] + lambda[0]*beta[i]; //GETI(i) (0)
                    H = QD[i] + lambda[0];   //GETI(i) (0)

                    // feature_node[] xi = prob.x[i];
                    // G += sparse_operator.dot(w, xi);
                    G += prob.x.dot(i, w);

                    double Gp = G+p;
                    double Gn = G-p;
                    double violation = 0;
                    if(beta[i] == 0)
                    {
                        if(Gp < 0)
                            violation = -Gp;
                        else if(Gn > 0)
                            violation = Gn;
                        else if(Gp>Gmax_old && Gn<-Gmax_old)
                        {
                            active_size--;
                            Helper.swap<int>(ref index[s], ref index[active_size]);
                            s--;
                            continue;
                        }
                    }
                    else if(beta[i] >= upper_bound[0])  //GETI(i) (0)
                    {
                        if(Gp > 0)
                            violation = Gp;
                        else if(Gp < -Gmax_old)
                        {
                            active_size--;
                            Helper.swap<int>(ref index[s], ref index[active_size]);
                            s--;
                            continue;
                        }
                    }
                    else if(beta[i] <= -upper_bound[0])  //GETI(i) (0)
                    {
                        if(Gn < 0)
                            violation = -Gn;
                        else if(Gn > Gmax_old)
                        {
                            active_size--;
                            Helper.swap<int>(ref index[s], ref index[active_size]);
                            s--;
                            continue;
                        }
                    }
                    else if(beta[i] > 0)
                        violation = Math.Abs(Gp);
                    else
                        violation = Math.Abs(Gn);

                    Gmax_new = Math.Max(Gmax_new, violation);
                    Gnorm1_new += violation;

                    // obtain Newton direction d
                    if(Gp < H*beta[i])
                        d = -Gp/H;
                    else if(Gn > H*beta[i])
                        d = -Gn/H;
                    else
                        d = -beta[i];

                    if(Math.Abs(d) < CONSTANTS.SMALL_ERR)
                        continue;

                    double beta_old = beta[i];
                    beta[i] = Math.Min(Math.Max(beta[i]+d, -upper_bound[0]), upper_bound[0]); //GETI(i) (0)
                    d = beta[i]-beta_old;

                    if(d != 0)
                        prob.x.axpy(d, i, w);
                }

                if(iter == 0)
                    Gnorm1_init = Gnorm1_new;
                iter++;
                if(iter % 10 == 0)
                    _logger.LogInformation(".");

                if(Gnorm1_new <= eps*Gnorm1_init)
                {
                    if(active_size == l)
                        break;
                    else
                    {
                        active_size = l;
                        _logger.LogInformation("*");
                        Gmax_old = double.PositiveInfinity;
                        continue;
                    }
                }

                Gmax_old = Gmax_new;
            }

            _logger.LogInformation("\noptimization finished, #iter = {0}\n", iter);
            if(iter >= max_iter)
                _logger.LogInformation("\nWARNING: reaching max number of iterations\nUsing -s 11 may be faster\n\n");

            // calculate objective value
            double v = 0;
            int nSV = 0;
            for(i=0; i<w_size; i++)
                v += w[i]*w[i];

            v = 0.5*v;
            for(i=0; i<l; i++)
            {
                v += p*Math.Abs(beta[i]) - y[i]*beta[i] + 0.5*lambda[0]*beta[i]*beta[i]; //GETI(i) (0)
                if(beta[i] != 0)
                    nSV++;
            }

            _logger.LogInformation("Objective value = {0}\n", v);
            _logger.LogInformation("nSV = {0}\n",nSV);

            return w;
        }
    }
}
        