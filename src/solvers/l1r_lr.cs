using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace liblinearcs {
    public class l1r_lr  {

        static RandomNumberGenerator rand = RandomNumberGenerator.Create(CONSTANTS.RANDOM_SEED);

        ILogger<l1r_lr> _logger;

        public l1r_lr() {
            _logger = ApplicationLogging.CreateLogger<l1r_lr>();
        }

        // A coordinate descent algorithm for
        // L1-regularized logistic regression problems
        //
        //  min_w \sum |wj| + C \sum log(1+exp(-yi w^T xi)),
        //
        // Given:
        // x, y, Cp, Cn
        // eps is the stopping tolerance
        //
        // solution will be put in w
        //
        // See Yuan et al. (2011) and appendix of LIBLINEAR paper, Fan et al. (2008)
        //
        // #undef GETI
        // #define GETI(i) (y[i]+1)
        // To support weights for instances, use GETI(i) (i)
        public void solve( Problem prob_col, ref double[] w, double eps, double Cp, double Cn)
        {
            int l = prob_col.l;
            int w_size = prob_col.n;
            int j, s, newton_iter=0, iter=0;
            int max_newton_iter = 100;
            int max_iter = 1000;
            int max_num_linesearch = 20;
            int active_size;
            int QP_active_size;

            double nu = CONSTANTS.SMALL_ERR;
            double inner_eps = 1;
            double sigma = 0.01;
            double w_norm, w_norm_new;
            double z, G, H;
            double Gnorm1_init = -1.0; // Gnorm1_init is initialized at the first iteration
            double Gmax_old = double.PositiveInfinity;
            double Gmax_new, Gnorm1_new;
            double QP_Gmax_old = double.PositiveInfinity;
            double QP_Gmax_new, QP_Gnorm1_new;
            double delta, negsum_xTd, cond;

            int[] index = new int[w_size];
            sbyte[] y = new sbyte[l];
            double[] Hdiag = new double[w_size];
            double[] Grad = new double[w_size];
            double[] wpd = new double[w_size];
            double[] xjneg_sum = new double[w_size];
            double[] xTd = new double[l];
            double[] exp_wTx = new double[l];
            double[] exp_wTx_new = new double[l];
            double[] tau = new double[l];
            double[] D = new double[l];
            //feature_node[] x;
            IEnumerable<SparseRowValue> x;

            double[] C = {Cn, 0, Cp};

            // Initial w can be set here.
            // for(j=0; j<w_size; j++)
            // 	w[j] = 0;
            Array.Clear(w, 0, w_size);

            for(j=0; j<l; j++)
            {
                y[j] = (sbyte)((prob_col.y[j] > 0) ? 1 : -1);

                exp_wTx[j] = 0;
            }

            w_norm = 0;
            for(j=0; j<w_size; j++)
            {
                w_norm += Math.Abs(w[j]);
                wpd[j] = w[j];
                index[j] = j;
                xjneg_sum[j] = 0;

                x = prob_col.x.getRow(j);
                foreach(SparseRowValue xk in x) {
                    int ind = xk.index;
                    double val = xk.value;
                    exp_wTx[ind] += w[j]*val;
                    if(y[ind] == -1)
                        xjneg_sum[j] += C[(y[ind]+1)]*val; // #define GETI(i) (y[i]+1)
                    
                }

                // x = prob_col.x[j];
                // int k = 0;
                // while(x[k].index != -1)
                // {
                //     int ind = x[k].index-1;
                //     double val = x[k].value;
                //     exp_wTx[ind] += w[j]*val;
                //     if(y[ind] == -1)
                //         xjneg_sum[j] += C[(y[ind]+1)]*val; // #define GETI(i) (y[i]+1)
                //     k++;
                // }
            }
            for(j=0; j<l; j++)
            {
                exp_wTx[j] = Math.Exp(exp_wTx[j]);
                double tau_tmp = 1/(1+exp_wTx[j]);
                tau[j] = C[y[j] + 1]*tau_tmp; // #define GETI(i) (y[i]+1)
                D[j] = C[y[j] + 1]*exp_wTx[j]*tau_tmp*tau_tmp; // #define GETI(i) (y[i]+1)
            }

            while(newton_iter < max_newton_iter)
            {
                Gmax_new = 0;
                Gnorm1_new = 0;
                active_size = w_size;

                for(s=0; s<active_size; s++)
                {
                    j = index[s];
                    Hdiag[j] = nu;
                    Grad[j] = 0;

                    double tmp = 0;

                    x = prob_col.x.getRow(j);
                    foreach(SparseRowValue xk in x) {
                        int ind = xk.index;
                        Hdiag[j] += xk.value*xk.value*D[ind];
                        tmp += xk.value*tau[ind];
                    }
                    // x = prob_col.x[j];
                    // int k = 0;
                    // while(x[k].index != -1)
                    // {
                    //     int ind = x[k].index-1;
                    //     Hdiag[j] += x[k].value*x[k].value*D[ind];
                    //     tmp += x[k].value*tau[ind];
                    //     k++;
                    // }
                    Grad[j] = -tmp + xjneg_sum[j];

                    double Gp = Grad[j]+1;
                    double Gn = Grad[j]-1;
                    double violation = 0;
                    if(w[j] == 0)
                    {
                        if(Gp < 0)
                            violation = -Gp;
                        else if(Gn > 0)
                            violation = Gn;
                        //outer-level shrinking
                        else if(Gp>Gmax_old/l && Gn<-Gmax_old/l)
                        {
                            active_size--;
                            Helper.swap<int>(ref index[s], ref index[active_size]);
                            s--;
                            continue;
                        }
                    }
                    else if(w[j] > 0)
                        violation = Math.Abs(Gp);
                    else
                        violation = Math.Abs(Gn);

                    Gmax_new = Math.Max(Gmax_new, violation);
                    Gnorm1_new += violation;
                }

                if(newton_iter == 0)
                    Gnorm1_init = Gnorm1_new;

                if(Gnorm1_new <= eps*Gnorm1_init)
                    break;

                iter = 0;
                QP_Gmax_old = Double.PositiveInfinity;
                QP_active_size = active_size;

                for(int i=0; i<l; i++)
                    xTd[i] = 0;

                // optimize QP over wpd
                while(iter < max_iter)
                {
                    QP_Gmax_new = 0;
                    QP_Gnorm1_new = 0;

                    for(j=0; j<QP_active_size; j++)
                    {
                        int i = j+rand.Next(QP_active_size-j);
                        Helper.swap<int>(ref index[i], ref index[j]);
                    }

                    for(s=0; s<QP_active_size; s++)
                    {
                        j = index[s];
                        H = Hdiag[j];

                        x = prob_col.x.getRow(j);
                        G = Grad[j] + (wpd[j]-w[j])*nu;
                        foreach(SparseRowValue xk in x) {
                            int ind = xk.index;
                            G += xk.value*D[ind]*xTd[ind];
                        }
                        // x = prob_col.x[j];
                        // G = Grad[j] + (wpd[j]-w[j])*nu;
                        // int k = 0;
                        // while(x[k].index != -1)
                        // {
                        //     int ind = x[k].index-1;
                        //     G += x[k].value*D[ind]*xTd[ind];
                        //     k++;
                        // }

                        double Gp = G+1;
                        double Gn = G-1;
                        double violation = 0;
                        if(wpd[j] == 0)
                        {
                            if(Gp < 0)
                                violation = -Gp;
                            else if(Gn > 0)
                                violation = Gn;
                            //inner-level shrinking
                            else if(Gp>QP_Gmax_old/l && Gn<-QP_Gmax_old/l)
                            {
                                QP_active_size--;
                                Helper.swap<int>(ref index[s], ref index[QP_active_size]);
                                s--;
                                continue;
                            }
                        }
                        else if(wpd[j] > 0)
                            violation = Math.Abs(Gp);
                        else
                            violation = Math.Abs(Gn);

                        QP_Gmax_new = Math.Max(QP_Gmax_new, violation);
                        QP_Gnorm1_new += violation;

                        // obtain solution of one-variable problem
                        if(Gp < H*wpd[j])
                            z = -Gp/H;
                        else if(Gn > H*wpd[j])
                            z = -Gn/H;
                        else
                            z = -wpd[j];

                        if(Math.Abs(z) < CONSTANTS.SMALL_ERR)
                            continue;
                        z = Math.Min(Math.Max(z,-10.0),10.0);

                        wpd[j] += z;

                        // x = prob_col.x[j];
                        // sparse_operator.axpy(z, x, xTd);
                        prob_col.x.axpy(z, j, xTd);
                    }

                    iter++;

                    if(QP_Gnorm1_new <= inner_eps*Gnorm1_init)
                    {
                        //inner stopping
                        if(QP_active_size == active_size)
                            break;
                        //active set reactivation
                        else
                        {
                            QP_active_size = active_size;
                            QP_Gmax_old = Double.PositiveInfinity;
                            continue;
                        }
                    }

                    QP_Gmax_old = QP_Gmax_new;
                }

                if(iter >= max_iter)
                    _logger.LogInformation("WARNING: reaching max number of inner iterations\n");

                delta = 0;
                w_norm_new = 0;
                for(j=0; j<w_size; j++)
                {
                    delta += Grad[j]*(wpd[j]-w[j]);
                    if(wpd[j] != 0)
                        w_norm_new += Math.Abs(wpd[j]);
                }
                delta += (w_norm_new-w_norm);

                negsum_xTd = 0;
                for(int i=0; i<l; i++)
                    if(y[i] == -1)
                        negsum_xTd += C[y[i]+1]*xTd[i];   // #define GETI(i) (y[i]+1)

                int num_linesearch;
                for(num_linesearch=0; num_linesearch < max_num_linesearch; num_linesearch++)
                {
                    cond = w_norm_new - w_norm + negsum_xTd - sigma*delta;

                    for(int i=0; i<l; i++)
                    {
                        double exp_xTd = Math.Exp(xTd[i]);
                        exp_wTx_new[i] = exp_wTx[i]*exp_xTd;
                        cond += C[y[i]+1]*Math.Log((1+exp_wTx_new[i])/(exp_xTd+exp_wTx_new[i])); // #define GETI(i) (y[i]+1)
                    }

                    if(cond <= 0)
                    {
                        w_norm = w_norm_new;
                        for(j=0; j<w_size; j++)
                            w[j] = wpd[j];
                        for(int i=0; i<l; i++)
                        {
                            exp_wTx[i] = exp_wTx_new[i];
                            double tau_tmp = 1/(1+exp_wTx[i]);
                            tau[i] = C[y[i]+1]*tau_tmp;  // #define GETI(i) (y[i]+1)
                            D[i] = C[y[i]+1]*exp_wTx[i]*tau_tmp*tau_tmp;  // #define GETI(i) (y[i]+1)
                        }
                        break;
                    }
                    else
                    {
                        w_norm_new = 0;
                        for(j=0; j<w_size; j++)
                        {
                            wpd[j] = (w[j]+wpd[j])*0.5;
                            if(wpd[j] != 0)
                                w_norm_new += Math.Abs(wpd[j]);
                        }
                        delta *= 0.5;
                        negsum_xTd *= 0.5;
                        for(int i=0; i<l; i++)
                            xTd[i] *= 0.5;
                    }
                }

                // Recompute some info due to too many line search steps
                if(num_linesearch >= max_num_linesearch)
                {
                    for(int i=0; i<l; i++)
                        exp_wTx[i] = 0;

                    for(int i=0; i<w_size; i++)
                    {
                        if(w[i]==0) continue;
                        // x = prob_col.x[i];
                        // sparse_operator.axpy(w[i], x, exp_wTx);
                        prob_col.x.axpy(w[i], i, exp_wTx);
                    }

                    for(int i=0; i<l; i++)
                        exp_wTx[i] = Math.Exp(exp_wTx[i]);
                }

                if(iter == 1)
                    inner_eps *= 0.25;

                newton_iter++;
                Gmax_old = Gmax_new;

                _logger.LogInformation("iter %3d  #CD cycles {0}\n", newton_iter, iter);
            }

            _logger.LogInformation("=========================\n");
            _logger.LogInformation("optimization finished, #iter = {0}\n", newton_iter);
            if(newton_iter >= max_newton_iter)
                _logger.LogInformation("WARNING: reaching max number of iterations\n");

            // calculate objective value

            double v = 0;
            int nnz = 0;
            for(j=0; j<w_size; j++)
                if(w[j] != 0)
                {
                    v += Math.Abs(w[j]);
                    nnz++;
                }
            for(j=0; j<l; j++)
                if(y[j] == 1)
                    v += C[y[j]+1]*Math.Log(1+1/exp_wTx[j]);  // #define GETI(i) (y[i]+1)
                else
                    v += C[y[j]+1]*Math.Log(1+exp_wTx[j]);   // #define GETI(i) (y[i]+1)

            _logger.LogInformation("Objective value = {0}\n", v);
            _logger.LogInformation("#nonzeros/#features = {0}/{1}\n", nnz, w_size);

            return ;
        }
    }
}