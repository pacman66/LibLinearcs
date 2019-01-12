using System;
using Microsoft.Extensions.Logging;

namespace liblinear {

    // A coordinate descent algorithm for
    // multi-class support vector machines by Crammer and Singer
    //
    //  min_{\alpha}  0.5 \sum_m ||w_m(\alpha)||^2 + \sum_i \sum_m e^m_i alpha^m_i
    //    s.t.     \alpha^m_i <= C^m_i \forall m,i , \sum_m \alpha^m_i=0 \forall i
    //
    //  where e^m_i = 0 if y_i  = m,
    //        e^m_i = 1 if y_i != m,
    //  C^m_i = C if m  = y_i,
    //  C^m_i = 0 if m != y_i,
    //  and w_m(\alpha) = \sum_i \alpha^m_i x_i
    //
    // Given:
    // x, y, C
    // eps is the stopping tolerance
    //
    // solution will be put in w
    //
    // See Appendix of LIBLINEAR paper, Fan et al. (2008)

    // #define GETI(i) ((int) prob->y[i])
    // To support weights for instances, use GETI(i) (i)

    class Solver_MCSVM_CS
    {  
        double[] B;
        double[] C;
        double[] G;
        int w_size, l;
        int nr_class;
        int max_iter;
        double eps;
        readonly Problem prob;

        private readonly ILogger<Solver_MCSVM_CS> _logger;

        //static MersenneTwister rand = new MersenneTwister();
        static RandomNumberGenerator rand = RandomNumberGenerator.Create(CONSTANTS.RANDOM_SEED);
    
        public Solver_MCSVM_CS(Problem prob, int nr_class, double[] weighted_C, double eps=0.1, int max_iter=100000 ) {
            this.w_size = prob.n;
            this.l = prob.l;
            this.nr_class = nr_class;
            this.eps = eps;
            this.max_iter = max_iter;
            this.prob = prob;
            this.B = new double[nr_class];
            this.G = new double[nr_class];
            this.C = weighted_C;
            this._logger = ApplicationLogging.CreateLogger<Solver_MCSVM_CS>();
        }
        public void Solve(double[] w) {
            int i, m, s;
            int l_nrclass = l * nr_class;
            int iter = 0;
            double[] alpha =  new double[l_nrclass];
            int[] alpha_index = new int[l_nrclass];
            double[] alpha_new = new double[nr_class];
            int[] index = new int[l];
            double[] QD = new double[l];
            int[] d_ind = new int[nr_class];
            double[] d_val = new double[nr_class];
            
            int[] y_index = new int[l];
            int active_size = l;
            byte[] active_size_i = new byte[l];
            double eps_shrink = Math.Max(10.0 * eps, 1.0); // stopping tolerance for shrinking
            bool start_from_all = true;

            // Initial alpha can be set here. Note that
            // sum_m alpha[i*nr_class+m] = 0, for all i=1,...,l-1
            // alpha[i*nr_class+m] <= C[GETI(i)] if prob->y[i] == m
            // alpha[i*nr_class+m] <= 0 if prob->y[i] != m
            // If initial alpha isn't zero, uncomment the for loop below to initialize w
            
            Array.Clear(alpha, 0, l_nrclass);

            Array.Clear(w, 0, w_size*nr_class);

            // Parallel.For(0,l, value  => {  
            //     for(int m2 = 0; m2 < nr_class; m2++)
            //         alpha_index[(value * nr_class) + m2] = m2;

            //     QD[value] = prob.x.nrm2_sq(value);

            //     active_size_i[value] = (byte)nr_class;
            //     y_index[value] = (int)prob.y[value];
            //     index[value] = value;
            //  });           

            for(i = 0; i < l; i++)
            {
                for(m = 0; m < nr_class; m++)
                    alpha_index[(i * nr_class) + m] = m;

                QD[i] = prob.x.nrm2_sq(i);

                active_size_i[i] = (byte)nr_class;
                y_index[i] = (int)prob.y[i];
                index[i] = i;
            }

            Codetimer ct = new Codetimer();
            double QDi, stopping, minG, maxG, d;
            int yi, inProbY, w_i_idx, nz_d, iNrclass, asi;
  
            while(iter < max_iter)
            {                
                stopping = double.NegativeInfinity ;
                for(i = 0; i < active_size; i++)
                    {
                        //int j = i + (int)(rand.genrand_real1()*remSize) ;
                        int j = i + rand.Next(active_size - i);
                        Helper.swap(index, i, j);
                    }
                for(s = 0; s < active_size; s++)
                {
                    i = index[s];
                    QDi = QD[i];
                    yi = y_index[i];
                    inProbY = (int)prob.y[i];
                    iNrclass = i * nr_class;
                    asi= active_size_i[i];

                    if( QDi > 0)
                    {
                        for(m = 0; m < asi; m++)
                            G[m] = 1;
                            
                        if(yi < asi)
                            G[yi] = 0;

                        RowArrayPtr<SparseRowValue> rap = prob.x.getRowPtr(i);
                        for(int j = 0; j < rap.Length; j++) {
                            w_i_idx = rap.get(j).index * nr_class;
                            for(m = 0; m < asi-1; m+=2) {
                                G[m] += w[w_i_idx + alpha_index[ iNrclass + m ]] * rap.get(j).value;
                                G[m+1] += w[w_i_idx + alpha_index[ iNrclass + m + 1]] * rap.get(j).value;
                            }
                            if (m < asi)
                                G[m] += w[w_i_idx + alpha_index[ iNrclass + m ]] * rap.get(j).value;
                        }            

                        minG = Double.PositiveInfinity;
                        maxG = Double.NegativeInfinity;
                        for(m = 0; m < asi; m++)
                        {
                            if( G[m] < minG && alpha[iNrclass + alpha_index[iNrclass + m]] < 0)
                                minG = G[m];
                            if(G[m] > maxG)
                                maxG = G[m];
                        }

                        if(yi < asi)
                            if( G[yi] < minG && G[yi] < minG && alpha[iNrclass + inProbY] < C[inProbY] )
                                minG = G[yi];

                        
                        for(m = 0; m <asi; m++)
                        {
                            if(be_shrunk(i, m, yi, alpha[iNrclass + alpha_index[iNrclass+m]], minG))
                            {
                                asi--;

                                while(asi > m)
                                {
                                    if(!be_shrunk(i, asi, yi,
                                                    alpha[iNrclass + alpha_index[iNrclass + asi]], minG))
                                    {
                                        Helper.swap(alpha_index, iNrclass + m, iNrclass + asi);
                                        Helper.swap(G, m, asi);
                                        if(yi == asi)
                                            yi = y_index[i] = m;
                                        else if(yi == m)
                                            yi = y_index[i] = asi;
                                        break;
                                    }
                                    asi--;
                                }
                            }
                        }
                        active_size_i[i] = (byte)asi;

                        if(asi <= 1)
                        {
                            active_size--;
                            Helper.swap(index, s, active_size);
                            s--;
                            continue;
                        }

                        if(maxG - minG > CONSTANTS.SMALL_ERR) {
                            stopping = Math.Max(maxG - minG, stopping);
                        
                            for(m = 0; m < asi; m++)
                                B[m] = G[m] -  QDi * alpha[iNrclass + alpha_index[iNrclass + m]] ;

                            solve_sub_problem( QDi, yi, C[inProbY], asi, alpha_new);
                        
                            nz_d = 0;
                            for(m = 0; m < asi; m++)
                            {
                                d = alpha_new[m] - alpha[iNrclass + alpha_index[iNrclass + m]];
                                alpha[iNrclass + alpha_index[iNrclass + m]] = alpha_new[m];
                                if(Math.Abs(d) > CONSTANTS.SMALL_ERR)
                                {
                                    d_ind[nz_d] = alpha_index[iNrclass + m];
                                    d_val[nz_d] = d;
                                    nz_d++;
                                }
                            }
                        
                            rap = prob.x.getRowPtr(i);
                            for(int j = 0; j < rap.Length; j++) {
                                w_i_idx = rap.get(j).index * nr_class;
                                for(m = 0; m < nz_d; m++)
                                    w[w_i_idx + d_ind[m]] += d_val[m] * rap.get(j).value;
                            }
                        }
                    }
                  
                }
                iter++;
                _logger.LogInformation("Loop Time {0}ms", ct.getTime());
                ct.reset();

                if(iter % 10 == 0)
                {
                    Console.Write(".");
                    //_logger.LogInformation(".");
                }

                if(stopping < eps_shrink)
                {
                    if(stopping < eps && start_from_all == true)
                        break;
                    else
                    {
                        active_size = l;
                        // for(i = 0; i < l; i++)
                        //     active_size_i[i] = nr_class;
                        Util.Memset(active_size_i, (byte)nr_class, l);
                        Console.Write("*");
                        //_logger.LogInformation("*");
                        eps_shrink = Math.Max(eps_shrink / 2, eps);
                        start_from_all = true;
                    }
                }
                else
                    start_from_all = false;
            }

            Console.WriteLine();
            _logger.LogInformation("optimization finished, #iter = {0}",iter);
            if (iter >= max_iter)
                _logger.LogInformation("WARNING: reaching max number of iterations");

            // calculate objective value
            double v = 0;
            int nSV = 0;
            for(i = 0; i < w_size * nr_class; i++)
                v += w[i] * w[i];
            v = 0.5 * v;
            for(i = 0; i < l_nrclass; i++)
            {
                v += alpha[i];
                if(Math.Abs(alpha[i]) > 0)
                    nSV++;
            }

            int nr_class_ctr = 0;
            for(i = 0; i < l; i++, nr_class_ctr+=nr_class)
                v -= alpha[nr_class_ctr + (int)prob.y[i]];
            _logger.LogInformation("Objective value = {0}",v);
            _logger.LogInformation("nSV = {0}",nSV);

        }
        private void solve_sub_problem(double A_i, int yi, double C_yi, int active_i, double[] alpha_new) {
            int r;
            double[] D = null;

            Helper.Clone(ref D, B, active_i);
            if(yi < active_i)
                D[yi] += A_i * C_yi;
            
            Helper.isortd(D);

            double beta = D[0] - A_i * C_yi;
            for(r = 1; r < active_i && beta < r * D[r]; r++)
                beta += D[r];
            beta /= r;

            for(r=0;r<active_i;r++)
            {
                alpha_new[r] = (r == yi) ? Math.Min(C_yi, (beta - B[r]) / A_i) : Math.Min((double)0, (beta - B[r]) / A_i);
            }
        }
        private bool be_shrunk(int i, int m, int yi, double alpha_i, double minG) {
            double bound = (m == yi) ? C[((int)prob.y[i])] : 0;
            return (alpha_i == bound && G[m] < minG);
        }
    }
}