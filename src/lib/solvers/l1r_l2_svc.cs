using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace liblinear {
    public class l1r_l2_svc  {

        static RandomNumberGenerator rand = RandomNumberGenerator.Create(CONSTANTS.RANDOM_SEED);

        ILogger<l1r_l2_svc> _logger;

        public l1r_l2_svc() {
            _logger = ApplicationLogging.CreateLogger<l1r_l2_svc>();
        }

        // A coordinate descent algorithm for
        // L1-regularized L2-loss support vector classification
        //
        //  min_w \sum |wj| + C \sum max(0, 1-yi w^T xi)^2,
        //
        // Given:
        // x, y, Cp, Cn
        // eps is the stopping tolerance
        //
        // solution will be put in w
        //
        // See Yuan et al. (2010) and appendix of LIBLINEAR paper, Fan et al. (2008)
        //
        // #undef GETI
        // #define GETI(i) (y[i]+1)
        // To support weights for instances, use GETI(i) (i)
        public void solve(Problem prob_col, ref double[] w, double eps, double Cp, double Cn)
        {
            int l = prob_col.l;
            int w_size = prob_col.n;
            int j, s, iter = 0;
            int max_iter = 1000;
            int active_size = w_size;
            int max_num_linesearch = 20;

            double sigma = 0.01;
            double d, G_loss, G, H;
            double Gmax_old = Double.PositiveInfinity;
            double Gmax_new, Gnorm1_new;
            double Gnorm1_init = -1.0; // Gnorm1_init is initialized at the first iteration
            double d_old, d_diff;
            double loss_old = 0, loss_new = 0;
            double appxcond, cond;

            int[] index = new int[w_size];
            sbyte[] y = new sbyte[l];
            double[] b = new double[l]; // b = 1-ywTx
            double[] xj_sq = new double[w_size];
//            feature_node[] x;
            IEnumerable<SparseRowValue> x;

            double[] C = {Cn, 0, Cp};

            // Initial w can be set here.
            // for(j=0; j<w_size; j++)
            // 	w[j] = 0;
            Array.Clear(w, 0, w_size);

            for(j=0; j<l; j++)
            {
                b[j] = 1;
                y[j] = (sbyte)((prob_col.y[j] > 0) ? 1 : -1);
            }
            for(j=0; j<w_size; j++)
            {
                index[j] = j;
                xj_sq[j] = 0;
                //x = prob_col.x[j];
                x =  prob_col.x.getRow(j);
                //int k = 0;
                foreach(SparseRowValue xk in x) {
                    int ind = xk.index;
                    //xk.value *= y[ind]; // x->value stores yi*xij
                    double val = xk.value * y[ind];
                    prob_col.x.At(j,xk.index,val);
                    b[ind] -= w[j]*val;
                    xj_sq[j] += C[y[ind]+1]*val*val; // #define GETI(i) (y[i]+1)

                }
                // while(x[k].index != -1)
                // {
                //     int ind = x[k].index-1;
                //     x[k].value *= y[ind]; // x->value stores yi*xij
                //     double val = x[k].value;
                //     b[ind] -= w[j]*val;
                //     xj_sq[j] += C[y[ind]+1]*val*val; // #define GETI(i) (y[i]+1)
                //     k++;
                // }
            }

            while(iter < max_iter)
            {
                Gmax_new = 0;
                Gnorm1_new = 0;

                for(j=0; j<active_size; j++)
                {
                    int i = j+rand.Next(active_size-j);
                    Helper.swap<int>(ref index[i], ref index[j]);
                }

                for(s=0; s<active_size; s++)
                {
                    j = index[s];
                    G_loss = 0;
                    H = 0;

                    // x = prob_col.x[j];
                    // int k = 0;
                    x = prob_col.x.getRow(j);

                    foreach(SparseRowValue xk in x) {
                        int ind = xk.index;
                        if(b[ind] > 0)
                        {
                            double val = xk.value;
                            double tmp = C[(y[ind]+1)]*val; // #define GETI(i) (y[i]+1)
                            G_loss -= tmp*b[ind];
                            H += tmp*val;
                        }
                    }
                    // while(x[k].index != -1)
                    // {
                    //     int ind = x[k].index-1;
                    //     if(b[ind] > 0)
                    //     {
                    //         double val = x[k].value;
                    //         double tmp = C[(y[ind]+1)]*val; // #define GETI(i) (y[i]+1)
                    //         G_loss -= tmp*b[ind];
                    //         H += tmp*val;
                    //     }
                    //     k++;
                    // }
                    G_loss *= 2;

                    G = G_loss;
                    H *= 2;
                    H =  Math.Max(H, CONSTANTS.SMALL_ERR) ;

                    double Gp = G+1;
                    double Gn = G-1;
                    double violation = 0;
                    if(w[j] == 0)
                    {
                        if(Gp < 0)
                            violation = -Gp;
                        else if(Gn > 0)
                            violation = Gn;
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

                    // obtain Newton direction d
                    if(Gp < H*w[j])
                        d = -Gp/H;
                    else if(Gn > H*w[j])
                        d = -Gn/H;
                    else
                        d = -w[j];

                    if(Math.Abs(d) < CONSTANTS.SMALL_ERR)
                        continue;

                    double delta = Math.Abs(w[j]+d)-Math.Abs(w[j]) + G*d;
                    d_old = 0;
                    int num_linesearch;
                    for(num_linesearch=0; num_linesearch < max_num_linesearch; num_linesearch++)
                    {
                        d_diff = d_old - d;
                        cond = Math.Abs(w[j]+d)-Math.Abs(w[j]) - sigma*delta;

                        appxcond = xj_sq[j]*d*d + G_loss*d + cond;
                        if(appxcond <= 0)
                        {
                            // x = prob_col.x[j];
                            // sparse_operator.axpy(d_diff, x, b);
                            prob_col.x.axpy(d_diff, j, b);
                            break;
                        }

                        if(num_linesearch == 0)
                        {
                            loss_old = 0;
                            loss_new = 0;

                            x = prob_col.x.getRow(j);

                            foreach(SparseRowValue xk in x) {
                                int ind = xk.index;
                                if(b[ind] > 0)
                                    loss_old += C[(y[ind]+1)]*b[ind]*b[ind]; // #define GETI(i) (y[i]+1)
                                double b_new = b[ind] + d_diff*xk.value;
                                b[ind] = b_new;
                                if(b_new > 0)
                                    loss_new += C[(y[ind]+1)]*b_new*b_new; // #define GETI(i) (y[i]+1)
                            }

                            // x = prob_col.x[j];
                            // k = 0;
                            // while(x[k].index != -1)
                            // {
                            //     int ind = x[k].index-1;
                            //     if(b[ind] > 0)
                            //         loss_old += C[(y[ind]+1)]*b[ind]*b[ind]; // #define GETI(i) (y[i]+1)
                            //     double b_new = b[ind] + d_diff*x[k].value;
                            //     b[ind] = b_new;
                            //     if(b_new > 0)
                            //         loss_new += C[(y[ind]+1)]*b_new*b_new; // #define GETI(i) (y[i]+1)
                            //     k++;
                            // }
                        }
                        else
                        {
                            loss_new = 0;
                            x = prob_col.x.getRow(j);

                            foreach(SparseRowValue xk in x) {
                                int ind = xk.index;
                                double b_new = b[ind] + d_diff*xk.value;
                                b[ind] = b_new;
                                if(b_new > 0)
                                    loss_new += C[(y[ind]+1)]*b_new*b_new; // #define GETI(i) (y[i]+1)
                            }                                
                            // x = prob_col.x[j];
                            // k = 0;
                            // while(x[k].index != -1)
                            // {
                            //     int ind = x[k].index-1;
                            //     double b_new = b[ind] + d_diff*x[k].value;
                            //     b[ind] = b_new;
                            //     if(b_new > 0)
                            //         loss_new += C[(y[ind]+1)]*b_new*b_new; // #define GETI(i) (y[i]+1)
                            //     k++;
                            // }
                        }

                        cond = cond + loss_new - loss_old;
                        if(cond <= 0)
                            break;
                        else
                        {
                            d_old = d;
                            d *= 0.5;
                            delta *= 0.5;
                        }
                    }

                    w[j] += d;

                    // recompute b[] if line search takes too many steps
                    if(num_linesearch >= max_num_linesearch)
                    {
                        _logger.LogInformation("#");
                        for(int i=0; i<l; i++)
                            b[i] = 1;

                        for(int i=0; i<w_size; i++)
                        {
                            if(w[i]==0) continue;
                            // x = prob_col.x[i];
                            // sparse_operator.axpy(-w[i], x, b);
                            prob_col.x.axpy(-w[i], i, b);
                        }
                    }
                }

                if(iter == 0)
                    Gnorm1_init = Gnorm1_new;
                iter++;
                if(iter % 10 == 0)
                    _logger.LogInformation(".");

                if(Gnorm1_new <= eps*Gnorm1_init)
                {
                    if(active_size == w_size)
                        break;
                    else
                    {
                        active_size = w_size;
                        _logger.LogInformation("*");
                        Gmax_old = double.PositiveInfinity;
                        continue;
                    }
                }

                Gmax_old = Gmax_new;
            }

            _logger.LogInformation("\noptimization finished, #iter = {0}\n", iter);
            if(iter >= max_iter)
                _logger.LogInformation("\nWARNING: reaching max number of iterations\n");

            // calculate objective value

            double v = 0;
            int nnz = 0;
            for(j=0; j<w_size; j++)
            {
                x = prob_col.x.getRow(j);

                foreach(SparseRowValue xk in x) {
                    prob_col.x.At(j, xk.index, xk.value * prob_col.y[xk.index] ); // restore x->value
                }                
                
                // x = prob_col.x[j];
                // int k = 0;
                // while(x[k].index != -1)
                // {
                //     x[k].value *= prob_col.y[x[k].index-1]; // restore x->value
                //     k++;
                // }
                if(w[j] != 0)
                {
                    v += Math.Abs(w[j]);
                    nnz++;
                }
            }
            for(j=0; j<l; j++)
                if(b[j] > 0)
                    v += C[(y[j]+1)]*b[j]*b[j];  // #define GETI(i) (y[i]+1)

            _logger.LogInformation("Objective value = {0}\n", v);
            _logger.LogInformation("#nonzeros/#features = {0}/{1}\n", nnz, w_size);

            return ;
        }
    }
}