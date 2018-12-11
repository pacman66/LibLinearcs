using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace liblinearcs {
    public class l2r_l1l2_svc  {

        static RandomNumberGenerator rand = RandomNumberGenerator.Create(CONSTANTS.RANDOM_SEED);

        ILogger<l2r_l1l2_svc> _logger;

        public l2r_l1l2_svc() {
            _logger = ApplicationLogging.CreateLogger<l2r_l1l2_svc>();
        }



        // A coordinate descent algorithm for
        // L1-loss and L2-loss SVM dual problems
        //
        //  min_\alpha  0.5(\alpha^T (Q + D)\alpha) - e^T \alpha,
        //    s.t.      0 <= \alpha_i <= upper_bound_i,
        //
        //  where Qij = yi yj xi^T xj and
        //  D is a diagonal matrix
        //
        // In L1-SVM case:
        // 		upper_bound_i = Cp if y_i = 1
        // 		upper_bound_i = Cn if y_i = -1
        // 		D_ii = 0
        // In L2-SVM case:
        // 		upper_bound_i = INF
        // 		D_ii = 1/(2*Cp)	if y_i = 1
        // 		D_ii = 1/(2*Cn)	if y_i = -1
        //
        // Given:
        // x, y, Cp, Cn
        // eps is the stopping tolerance
        //
        // solution will be put in w
        //
        // See Algorithm 3 of Hsieh et al., ICML 2008
        //
        // #undef GETI
        // #define GETI(i) (y[i]+1)
        // To support weights for instances, use GETI(i) (i)
        // Adjusted for BIAS not in matrix
        public double[] solve(	Problem prob, double[] w, double eps,	double Cp, double Cn, SOLVER_TYPE solver_type)
        {
            int l = prob.l;
            int w_size = prob.n;
            int i, s, iter = 0;
            double C, d, G;
            double[] QD = new double[l];
            int max_iter = 1000;
            int[] index = new int[l];
            double[] alpha = new double[l];
            sbyte[] y = new sbyte[l];
            int active_size = l;

            Console.WriteLine("prob n = " + prob.n);

            // PG: projected gradient, for shrinking and stopping
            double PG;
            double PGmax_old = double.PositiveInfinity;
            double PGmin_old = double.NegativeInfinity;
            double PGmax_new;
            double PGmin_new;

            // default solver_type: L2R_L2LOSS_SVC_DUAL
            double[] diag = {0.5/Cn, 0, 0.5/Cp};
            double[] upper_bound = {double.PositiveInfinity, 0, double.PositiveInfinity};
            if(solver_type == SOLVER_TYPE.L2R_L1LOSS_SVC_DUAL)
            {
                diag[0] = 0;
                diag[2] = 0;
                upper_bound[0] = Cn;
                upper_bound[2] = Cp;
            }

            for(i = 0; i < l; i++)
            {
                y[i] = (sbyte) ((prob.y[i] > 0) ? 1 : -1);
            }

            // Initial alpha can be set here. Note that
            // 0 <= alpha[i] <= upper_bound[GETI(i)]
            // for(i=0; i<l; i++)
            // 	alpha[i] = 0;
            Array.Clear(alpha, 0, l);

            // for(i=0; i<w_size; i++)
            // 	w[i] = 0;
            Array.Clear(w, 0, w_size);

            for(i = 0; i < l; i++)
            {
                QD[i] = diag[(y[i] + 1)]; //GET(i)

                //feature_node[] xi = prob.x[i];
                //QD[i] += sparse_operator.nrm2_sq(xi);
                //sparse_operator.axpy(y[i]*alpha[i], xi, w);
                QD[i] += prob.x.nrm2_sq(i);
                prob.x.axpy(y[i] * alpha[i], i, w);

                index[i] = i;
            }

            while (iter < max_iter)
            {
                PGmax_new = double.NegativeInfinity;
                PGmin_new = double.PositiveInfinity;

                for (i=0; i<active_size; i++)
                {
                    int j = i + rand.Next(active_size - i);
                    Helper.swap<int>(ref index[i], ref index[j]);
                }

                for (s=0; s<active_size; s++)
                {
                    i = index[s];
                    sbyte yi = y[i];
                    
                    //feature_node[] xi = prob.x[i];

                    G = (yi * prob.x.dot(i, w)) - 1;
                    // G = yi*sparse_operator.dot(w, xi)-1;

                    C = upper_bound[(yi+1)];   // GETI
                    G += alpha[i] * diag[(yi+1)]; // GETI

                    PG = 0;
                    if (Math.Abs(alpha[i]) < Double.Epsilon)
                    //if (alpha[i] == 0)
                    {
                        if (G > PGmax_old)
                        {
                            active_size--;
                            Helper.swap<int>(ref index[s], ref index[active_size]);
                            s--;
                            continue;
                        }
                        else if (G < 0)
                            PG = G;
                    }
                    else if (Math.Abs(alpha[i]-C) < Double.Epsilon) //if (alpha[i] == C)
                    {
                        if (G < PGmin_old)
                        {
                            active_size--;
                            Helper.swap<int>(ref index[s], ref index[active_size]);
                            s--;
                            continue;
                        }
                        else if (G > 0)
                            PG = G;
                    }
                    else
                        PG = G;

                    PGmax_new = Math.Max(PGmax_new, PG);
                    PGmin_new = Math.Min(PGmin_new, PG);

                    if(Math.Abs(PG) > CONSTANTS.SMALL_ERR)
                    {
                        double alpha_old = alpha[i];
                        alpha[i] = Math.Min(Math.Max(alpha[i] - G/QD[i], 0.0), C);
                        d = (alpha[i] - alpha_old)*yi;
                        prob.x.axpy(d, i, w);
                        
                    }
                }

                iter++;
                if(iter % 10 == 0)
                    _logger.LogInformation(".");

                if(PGmax_new - PGmin_new <= eps)
                {
                    if(active_size == l)
                        break;
                    else
                    {
                        active_size = l;
                        _logger.LogInformation("*");
                        PGmax_old = double.PositiveInfinity;
                        PGmin_old = double.NegativeInfinity;
                        continue;
                    }
                }
                PGmax_old = PGmax_new;
                PGmin_old = PGmin_new;
                if (PGmax_old <= 0)
                    PGmax_old = double.PositiveInfinity;
                if (PGmin_old >= 0)
                    PGmin_old = double.NegativeInfinity;
            }

            _logger.LogInformation("\noptimization finished, #iter = {0}\n",iter);
            if (iter >= max_iter)
                _logger.LogInformation("\nWARNING: reaching max number of iterations\nUsing -s 2 may be faster (also see FAQ)\n\n");

            // calculate objective value

            double v = 0;
            int nSV = 0;
            for(i=0; i<w_size; i++)
                v += w[i] * w[i];
            for(i=0; i<l; i++)
            {
                v += alpha[i] * (alpha[i] * diag[(y[i]+1)] - 2);  //GETI
                if(alpha[i] > 0)
                    ++nSV;
            }
            _logger.LogInformation("Objective value = {0}\n",v/2);
            _logger.LogInformation("nSV = {0}\n",nSV);
            return w;
        }

    }
}

