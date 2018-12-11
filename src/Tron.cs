using System;
using Microsoft.Extensions.Logging;

namespace liblinearcs {
    public class Tron {

        const double SML_VALUE = 1.0e-12;
        const double MIN_VALUE = -1.0e+32;
	    private double eps;
	    private double eps_cg;
	    private int max_iter;
        private IFunction fun_obj;
        private readonly ILogger<Tron> _logger;

        public Tron(IFunction fun_obj, double eps=0.1, double eps_cg=0.1, int max_iter=1000) {
            this.fun_obj=(fun_obj);
            this.eps=eps;
            this.eps_cg=eps_cg;
            this.max_iter=max_iter;
            _logger = ApplicationLogging.CreateLogger<Tron>();
        }


	    public void TrainOne(double[] w) {
            // Parameters for updating the iterates.
            double eta0 = 1e-4, eta1 = 0.25, eta2 = 0.75;

            // Parameters for updating the trust region size delta.
            double sigma1 = 0.25, sigma2 = 0.5, sigma3 = 4;

            int n = fun_obj.get_nr_variable();
            int i, cg_iter;
            double delta=0, sMnorm, one=1.0;
            double alpha, f, fnew, prered, actred, gs;
            bool search; 
            int iter = 1, inc = 1;
            double[] s = new double[n];
            double[] r = new double[n];
            double[] g = new double[n];

            const double alpha_pcg = 0.01;
            double[] M = new double[n];

            // calculate gradient norm at w=0 for stopping condition.
            double[] w0 = new double[n];
            Array.Clear(w0, 0, n);
            
            fun_obj.fun(w0);
            fun_obj.grad(w0, g);

            double gnorm0 = Blas.dnrm2_(n, g, inc);

            f = fun_obj.fun(w);
            fun_obj.grad(w, g);

            double gnorm = Blas.dnrm2_(n, g, inc);

            search = !(gnorm <= eps * gnorm0);

            fun_obj.get_diag_preconditioner(M);
            for(i = 0; i < n; i++)
                M[i] = (1 - alpha_pcg) + alpha_pcg * M[i];

            delta = Math.Sqrt(uTMv(n, g, M, g));

            double[] w_new = new double[n];
            Boolean reach_boundary = true;
            bool delta_adjusted = false;
            while (iter <= max_iter && search)
            {
                cg_iter = trpcg(delta, ref g, ref M, ref s, ref r, reach_boundary);

                for (int ti = 0; ti < n; ti++) {
                    w_new[ti] = w[ti];
                }
                Blas.daxpy_(n, one, s, inc, ref w_new, inc);

                gs = Blas.ddot_(n, g, inc, s, inc);
                prered = -0.5 * (gs - Blas.ddot_(n, s, inc, r, inc));
                fnew = fun_obj.fun(w_new);

                // Compute the actual reduction.
                actred = f - fnew;

                // On the first iteration, adjust the initial step bound.
                sMnorm = Math.Sqrt(uTMv(n, s, M, s));
                if (iter == 1 && !delta_adjusted)
                {
                    delta = Math.Min(delta, sMnorm);
                    delta_adjusted = true;
                }

                // Compute prediction alpha*sMnorm of the step.
                if (fnew - f - gs <= 0)
                    alpha = sigma3;
                else
                    alpha = Math.Max(sigma1, -0.5 * (gs / (fnew - f - gs)));

                // Update the trust region bound according to the ratio of actual to predicted reduction.
                if (actred < eta0 * prered)
                    delta = Math.Min(alpha * sMnorm, sigma2 * delta);
                else if (actred < eta1*prered)
                    delta = Math.Max(sigma1 * delta, Math.Min(alpha * sMnorm, sigma2 * delta));
                else if (actred < eta2*prered)
                    delta = Math.Max(sigma1 * delta, Math.Min(alpha * sMnorm, sigma3 * delta));
                else
                {
                    if (reach_boundary)
                        delta = sigma3 * delta;
                    else
                        delta = Math.Max(delta, Math.Min(alpha * sMnorm, sigma3 * delta));
                }

                _logger.LogTrace("iter {0} act {1} pre {2} delta {3} f {4} |g| {5} CG {6}", iter, actred, prered, delta, f, gnorm, cg_iter);

                if (actred > eta0 * prered)
                {
                    iter++;
                    for (int ti = 0; ti < n; ti++) {
                        w[ti] = w_new[ti];
                    }

                    f = fnew;
                    fun_obj.grad(w, g);
                    fun_obj.get_diag_preconditioner(M);
                    for(i = 0; i < n; i++)
                        M[i] = (1 - alpha_pcg) + alpha_pcg * M[i];

                    gnorm = Blas.dnrm2_(n, g, inc);
                    if (gnorm <= eps * gnorm0)
                        break;
                }
                if (f < MIN_VALUE)
                {
                    _logger.LogTrace("WARNING: f < {0.00e+00}", MIN_VALUE);
                    break;
                }
                if (prered <= 0)
                {
                    _logger.LogTrace("WARNING: prered <= 0");
                    break;
                }
                if (Math.Abs(actred) <= SML_VALUE * Math.Abs(f) &&
                    Math.Abs(prered) <= SML_VALUE * Math.Abs(f))
                {
                    _logger.LogTrace("WARNING: actred and prered too small");
                    break;
                }
            }
        }


	    private int trpcg(double delta, ref double[] g, ref double [] M, ref double[] s, ref double[] r, Boolean reach_boundary) {
          	int i, inc = 1;
            int n = fun_obj.get_nr_variable();
            double one = 1;
            double[] d = new double[n];
            double[] Hd = new double[n];
            double zTr, znewTrnew, alpha, beta, cgtol;
            double[] z = new double[n];

            reach_boundary = false;
            for (i = 0; i < n; i++)
            {
                s[i] = 0;
                r[i] = -g[i];
                z[i] = r[i] / M[i];
                d[i] = z[i];
            }

            zTr = Blas.ddot_(n, z, inc, r, inc);
            cgtol = eps_cg * Math.Sqrt(zTr);
            
            int cg_iter = 0;

            while (true)
            {
                if (Math.Sqrt(zTr) <= cgtol)
                    break;
                cg_iter++;
                fun_obj.Hv(d, Hd);

                alpha = zTr/Blas.ddot_(n, d, inc, Hd, inc);
                Blas.daxpy_(n, alpha, d, inc, ref s, inc);

                double sMnorm = Math.Sqrt(uTMv(n, s, M, s));
                if (sMnorm > delta)
                {
                    _logger.LogTrace("cg reaches trust region boundary");
                    reach_boundary = true;
                    alpha = -alpha;
                    Blas.daxpy_(n, alpha, d, inc, ref s, inc);

                    double sTMd = uTMv(n, s, M, d);
                    double sTMs = uTMv(n, s, M, s);
                    double dTMd = uTMv(n, d, M, d);
                    double dsq = delta*delta;
                    double rad = Math.Sqrt(sTMd*sTMd + dTMd*(dsq-sTMs));
                    if (sTMd >= 0)
                        alpha = (dsq - sTMs)/(sTMd + rad);
                    else
                        alpha = (rad - sTMd)/dTMd;
                    Blas.daxpy_(n, alpha, d, inc, ref s, inc);
                    alpha = -alpha;
                    Blas.daxpy_(n, alpha, Hd, inc, ref r, inc);
                    break;
                }
                alpha = -alpha;
                Blas.daxpy_(n, alpha, Hd, inc, ref r, inc);

                for (i = 0; i < n; i++)
                    z[i] = r[i] / M[i];
                znewTrnew = Blas.ddot_(n, z, inc, r, inc);
                beta = znewTrnew / zTr;
                Blas.dscal_(n, beta, ref d, inc);
                Blas.daxpy_(n, one, z, inc, ref d, inc);
                zTr = znewTrnew;
            }

            return(cg_iter);  
        }


        private double norm_inf(int n, double[] x) {
            double dmax = Math.Abs(x[0]);
            for (int i = 1; i < n; i++)
                if (Math.Abs(x[i]) >= dmax)
                    dmax = Math.Abs(x[i]);
            return(dmax);
        }

        static double uTMv(int n, double[] u, double[] M, double[] v)
        {
            int m = n - 4;
            double res = 0;
            int i;
            for (i = 0; i < m; i += 5)
                res +=  u[i] * M[i] * v[i] + 
                        u[i+1] * M[i+1] * v[i+1] + 
                        u[i+2] * M[i+2] * v[i+2] +
                        u[i+3] * M[i+3] * v[i+3] +
                        u[i+4] * M[i+4] * v[i+4];
            for (; i<n; i++)
                res += u[i] * M[i] * v[i];
            return res;
        }
    }

}