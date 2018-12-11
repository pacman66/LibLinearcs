using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using Microsoft.Extensions.Logging;
 
namespace liblinearcs {
    public class Linear {
        
        public static readonly  Encoding  FILE_CHARSET = Encoding.ASCII;
        public static readonly  CultureInfo  DEFAULT_LOCALE = CultureInfo.CurrentCulture;
        private static object      OUTPUT_MUTEX  = new object();
        private ILogger<Linear> _logger = null; 
        static RandomNumberGenerator rand = RandomNumberGenerator.Create(CONSTANTS.RANDOM_SEED);   

        public Linear()
        {
            this._logger = ApplicationLogging.CreateLogger<Linear>();
        }


        public void cross_validation(Problem prob, Parameter param, ref int nr_fold, ref double[] target) {
            int i;
            int l = prob.l;
            int[] perm = new int[l];

            if (nr_fold > l) {
                nr_fold = l;
                _logger.LogWarning("WARNING: # folds > # data. Will use # folds = # data instead (i.e., leave-one-out cross validation)");
            }
            int[] fold_start = new int[nr_fold + 1];

            for (i = 0; i < l; i++)
                perm[i] = i;

            for (i = 0; i < l; i++) {
                int j = i + rand.Next(l - i);
                Helper.swap(perm, i, j);
            }

            for (i = 0; i <= nr_fold; i++)
                fold_start[i] = i * l / nr_fold;

            for (i = 0; i < nr_fold; i++) {
                int begin = fold_start[i];
                int end = fold_start[i + 1];
                int j, k;
                Problem subprob = new Problem();

                subprob.bias = prob.bias;
                subprob.n = prob.n;
                subprob.l = l - (end - begin);
                
                subprob.y = new double[subprob.l];
                int[] newperm = new int[subprob.l];
                k = 0;
                for (j = 0; j < begin; j++) {
                    newperm[k] = perm[j];
                    subprob.y[k] = prob.y[perm[j]];
                    ++k;
                }
                for (j = end; j < l; j++) {
                    newperm[k] = perm[j];
                    subprob.y[k] = prob.y[perm[j]];
                    ++k;
                }
                subprob.x= SparseMatrixFactory.CreateFacadeMatrix(prob.x, newperm);

                Model submodel = train(subprob, param);
                for (j = begin; j < end; j++)
                    target[perm[j]] = predict(submodel, prob.x.getRowPtr(perm[j]));
            }
        }


        public void find_parameter_C(Problem prob, Parameter param1, int nr_fold, double start_C, double max_C, ref double best_C, ref double best_rate) {
            // variables for CV
            int i;
            int l = prob.l;
            int[] perm = new int[l];
            
            double[] target = new double[prob.l];
            Problem[] subprob = new Problem[nr_fold];

            // variables for warm start
            double ratio = 2;
            double[][] prev_w = new double[nr_fold][];
            int num_unchanged_w = 0;
            //parameter param1 = param;

            if (nr_fold > l) {
                nr_fold = l;
                _logger.LogWarning("WARNING: # folds > # data. Will use # folds = # data instead (i.e., leave-one-out cross validation)");
            }
            int[] fold_start = new int[nr_fold + 1];

            for (i = 0; i < l; i++) 
                perm[i] = i;

            for (i = 0; i < l; i++) {
                int j = i + rand.Next(l - i);
                Helper.swap(perm, i, j);
            }
            for (i = 0; i <= nr_fold; i++)
                fold_start[i] = i * l / nr_fold;

            for (i = 0; i < nr_fold; i++) {
                int begin = fold_start[i];
                int end = fold_start[i + 1];
                int j, k;

                subprob[i].bias = prob.bias;
                subprob[i].n = prob.n;
                subprob[i].l = l - (end - begin);

                int[] newperm = new int[subprob[i].l];               
                subprob[i].y = new double[subprob[i].l];

                k = 0;
                for (j = 0; j < begin; j++) {
                    newperm[k]=perm[j];
                    subprob[i].y[k] = prob.y[perm[j]];
                    ++k;
                }
                for (j = end; j < l; j++) {
                    newperm[k]=perm[j];
                    subprob[i].y[k] = prob.y[perm[j]];
                    ++k;
                }
                subprob[i].x = SparseMatrixFactory.CreateFacadeMatrix(prob.x, newperm);

            }

            //best_C = Double.NaN;
            best_rate = 0;
            if (start_C <= 0)
                start_C = calc_start_C(prob, param1);
            //start_C = calc_start_C(prob, param);
            param1.C = start_C;
            Console.WriteLine("Param1.C {0}", param1.C);

            while (param1.C <= max_C) {
                for (i = 0; i < nr_fold; i++) {
                    int j;
                    int begin = fold_start[i];
                    int end = fold_start[i + 1];

                    param1.init_sol = prev_w[i];
                    Model submodel = train(subprob[i], param1);

                    int total_w_size;
                    if (submodel.nr_class == 2)
                        total_w_size = subprob[i].n;
                    else
                        total_w_size = subprob[i].n * submodel.nr_class;

                    if (prev_w[i] == null) {
                        prev_w[i] = new double[total_w_size];
                        for (j = 0; j < total_w_size; j++)
                            prev_w[i][j] = submodel.w[j];
                    } else if (num_unchanged_w >= 0) {
                        double norm_w_diff = 0;
                        for (j=0; j<total_w_size; j++) {
                            norm_w_diff += (submodel.w[j] - prev_w[i][j]) * (submodel.w[j] - prev_w[i][j]);
                            prev_w[i][j] = submodel.w[j];
                        }
                        //norm_w_diff = Math.Sqrt(norm_w_diff);

                        if (norm_w_diff > CONSTANTS.SMALL_EPS_SQ)
                            num_unchanged_w = -1;
                    } else {
                        for (j = 0; j < total_w_size; j++)
                            prev_w[i][j] = submodel.w[j];
                    }

                    for (j = begin; j < end; j++)
                        target[perm[j]] = predict(submodel, prob.x.getRowPtr(perm[j]));
                }

                int total_correct = 0;
                for (i = 0; i < prob.l; i++)
                    if (target[i] == prob.y[i])
                        ++total_correct;
                
                double current_rate = (double) total_correct / prob.l;
                if (current_rate > best_rate) {
                    best_C = param1.C;
                    best_rate = current_rate;
                }

                _logger.LogInformation("log2c={0}\trate={1}", Math.Log(param1.C) / Math.Log(2.0), 100.0 * current_rate);
                num_unchanged_w++;
                if (num_unchanged_w == 3)
                    break;
                param1.C = param1.C * ratio;
            }

            if (param1.C > max_C && max_C > start_C)
                _logger.LogInformation("warning: maximum C reached.\n");
        }


        public double predict_values(Model model, RowArrayPtr<SparseRowValue> x, double[] dec_values) {
            int n;
            if (model.bias >= 0)
                n = model.nr_feature + 1;
            else
                 n = model.nr_feature;

            double[] w = model.w;

            int nr_w;
            if (model.nr_class == 2 && model.param.solver_type != SOLVER_TYPE.MCSVM_CS)
                nr_w = 1;
            else
                nr_w = model.nr_class;

            for (int i = 0; i < nr_w; i++)
                dec_values[i] = 0;

            double val;
            int idx;
            for (int i = 0; i < x.Length; i++) {
                val = x.get(i).value;
                idx = x.get(i).index;
                
                // the dimension of testing data may exceed that of training
                if (idx <= n) {
                    idx *= nr_w;
                    for (int j = 0; j < nr_w; j++) {
                        dec_values[j] += w[idx + j] * val;
                    }
                }
            }

            if (model.nr_class == 2) {
                if (model.param.check_regression_model())
                    return dec_values[0];
                else
                    return (dec_values[0] > 0) ? model.label[0] : model.label[1];
            } else {
                int dec_max_idx = 0;
                for (int i = 1; i < model.nr_class; i++) {
                    if (dec_values[i] > dec_values[dec_max_idx]) dec_max_idx = i;
                }
                return model.label[dec_max_idx];
            }
        }

        public double predict(Model model, RowArrayPtr<SparseRowValue> x) {
            double[] dec_values = new double[model.nr_class];
            return predict_values(model, x, dec_values);
        }
        
        double predict_probability(Model model_, RowArrayPtr<SparseRowValue> x, double[] prob_estimates)
        {
            if(model_.param.check_probability_model())
            {
                int i;
                int nr_class = model_.nr_class;
                int nr_w;
                if(nr_class == 2)
                    nr_w = 1;
                else
                    nr_w = nr_class;

                double label = predict_values(model_, x, prob_estimates);
                for(i = 0; i < nr_w; i++)
                    prob_estimates[i] = 1 / (1 + Math.Exp(-prob_estimates[i]));

                if(nr_class == 2) // for binary classification
                    prob_estimates[1] = 1.0 - prob_estimates[0];
                else
                {
                    double sum=0;
                    for(i = 0; i < nr_class; i++)
                        sum += prob_estimates[i];

                    for(i = 0; i < nr_class; i++)
                        prob_estimates[i] = prob_estimates[i]/sum;
                }

                return label;
            }
            else
                return 0;
        }


        // transpose matrix X from row format to column format
        static public Problem transpose(Problem prob )
        {
            int i;
            int l = prob.l;
            int n = prob.n;
            //int [] col_ptr = new int[n+1];
            Problem prob_col = new Problem();
            prob_col.l = l;
            prob_col.n = n;
            prob_col.y = new double[l];
               
            for(i=0; i<l; i++)
                prob_col.y[i] = prob.y[i];
        
            prob_col.x = (SparseMatrix) prob.x.Transpose();

            return prob_col;
        }

        // label: label name, start: begin of each class, count: #data of classes, perm: indices to the original data
        // perm, length l, must be allocated before calling this subroutine
        static void group_classes(Problem prob, ref int nr_class_ret, ref int[] label_ret, ref int[] start_ret, ref int[] count_ret, ref int[] perm)
        {
            int l = prob.l;
            int max_nr_class = 16;
            int nr_class = 0;
            int[] label = new int[max_nr_class];
            int[] count = new int[max_nr_class];
            int[] data_label = new int[l];
            int i;

            for(i=0;i<l;i++)
            {
                int this_label = (int)prob.y[i];
                int j;
                for(j=0; j<nr_class; j++)
                {
                    if(this_label == label[j])
                    {
                        ++count[j];
                        break;
                    }
                }
                data_label[i] = j;
                if(j == nr_class)
                {
                    if(nr_class == max_nr_class)
                    {
                        max_nr_class *= 2;
                        Array.Resize(ref label, max_nr_class);
                        Array.Resize(ref count, max_nr_class);
                    }
                    label[nr_class] = this_label;
                    count[nr_class] = 1;
                    ++nr_class;
                }
            }

            //
            // Labels are ordered by their first occurrence in the training set.
            // However, for two-class sets with -1/+1 labels and -1 appears first,
            // we swap labels to ensure that internally the binary SVM has positive data corresponding to the +1 instances.
            //
            if (nr_class == 2 && label[0] == -1 && label[1] == 1)
            {
                Helper.swap(label, 0, 1);
                Helper.swap(count, 0, 1);
                for(i=0;i<l;i++)
                {
                    data_label[i] = (data_label[i] == 0) ? 1 : 0;
                }
            }

            int[] start = new int[nr_class];
            start[0] = 0;
            for(i=1; i<nr_class; i++)
                start[i] = start[i-1] + count[i-1];

            for(i=0; i<l; i++)
            {
                perm[start[data_label[i]]] = i;
                ++start[data_label[i]];
            }

            start[0] = 0;
            for(i=1; i<nr_class; i++)
                start[i] = start[i-1] + count[i-1];

            nr_class_ret = nr_class;
            label_ret = label;
            start_ret = start;
            count_ret = count;
        }

        void train_one(Problem prob, Parameter param, ref double[] w, double Cp, double Cn)
        {
            //inner and outer tolerances for TRON
            double eps = param.eps;
            double eps_cg = 0.1;
            if(param.init_sol != null)
                eps_cg = 0.5;

            int pos = 0;
            for(int i=0; i<prob.l; i++)
                if(prob.y[i] > 0)
                    pos++;

            int neg = prob.l - pos;
            double primal_solver_tol = eps*Math.Max(Math.Min(pos,neg), 1)/prob.l;

            IFunction fun_obj=null;
            switch(param.solver_type)
            {
                case SOLVER_TYPE.L2R_LR:
                {
                    double[] C = new double[prob.l];
                    for(int i = 0; i < prob.l; i++)
                    {
                        if(prob.y[i] > 0)
                            C[i] = Cp;
                        else
                            C[i] = Cn;
                    }
                    fun_obj = new l2r_lr_fun(prob, C);
                    Tron tron_obj = new Tron(fun_obj, primal_solver_tol, eps_cg);
                
                    tron_obj.TrainOne(w);
                    break;
                }
                case SOLVER_TYPE.L2R_L2LOSS_SVC:
                {
                    double[] C = new double[prob.l];
                    for(int i = 0; i < prob.l; i++)
                    {
                        if(prob.y[i] > 0)
                            C[i] = Cp;
                        else
                            C[i] = Cn;
                    }
                    fun_obj=new l2r_l2_svc_fun(prob, C);
                    Tron tron_obj = new Tron(fun_obj, primal_solver_tol, eps_cg);
                    tron_obj.TrainOne(w);
                    break;
                }
                case SOLVER_TYPE.L2R_L2LOSS_SVC_DUAL:
                case SOLVER_TYPE.L2R_L1LOSS_SVC_DUAL:
                    { 
                        l2r_l1l2_svc solver = new l2r_l1l2_svc();
                        solver.solve(prob, w, eps, Cp, Cn, param.solver_type);
                    }
                    break;
                case SOLVER_TYPE.L1R_L2LOSS_SVC:
                {
                    { 
                        Problem prob_col = transpose(prob);
                        l1r_l2_svc solver = new l1r_l2_svc();
                        solver.solve(prob_col, ref w, primal_solver_tol, Cp, Cn);
                    }
                    break;
                }
                case SOLVER_TYPE.L1R_LR:
                {
                    Problem prob_col = transpose(prob);
                    l1r_lr solver = new l1r_lr();
                    solver.solve(prob_col, ref w, primal_solver_tol, Cp, Cn);

                    break;
                }
                case SOLVER_TYPE.L2R_LR_DUAL:
                {
                    l2r_lr_dual solver = new l2r_lr_dual();
                    solver.solve(prob, ref w, eps, Cp, Cn);
                    break;
                }
                case SOLVER_TYPE.L2R_L2LOSS_SVR:
                {
                    double[] C = new double[prob.l];
                    for(int i = 0; i < prob.l; i++)
                        C[i] = param.C;

                    fun_obj=new l2r_l2_svr_fun(prob, C, param.p);
                    Tron tron_obj = new Tron(fun_obj, param.eps);
                    tron_obj.TrainOne(w);
                    break;

                }
                case SOLVER_TYPE.L2R_L1LOSS_SVR_DUAL:
                case SOLVER_TYPE.L2R_L2LOSS_SVR_DUAL:
                   {
                       l2r_l1l2_svr solver = new l2r_l1l2_svr();
                       solver.solve(prob, w, param, param.solver_type);
                   }
                   break;               
                default:
                    _logger.LogError("ERROR: unknown solver_type\n");
                    break;
            }
        }

        static double calc_start_C(Problem prob, Parameter param)
        {
            int i;
            double xTx,max_xTx;
            max_xTx = 0;
            for(i=0; i<prob.l; i++)
            {
                // xTx = 0;

                xTx = prob.x.nrm2_sq(i);

                // IEnumerable<SparseRowValue> xi = prob.x.getRow(i);
                // foreach ( SparseRowValue xik in xi ) {
                //     double val = xik.value;
                    
                //     xTx += val*val;
                // }
  
                if(xTx > max_xTx)
                    max_xTx = xTx;
            }
            double min_C = 1.0;
            if(param.solver_type == SOLVER_TYPE.L2R_LR)
                min_C = 1.0 / (prob.l * max_xTx);
            else if(param.solver_type == SOLVER_TYPE.L2R_L2LOSS_SVC)
                min_C = 1.0 / (2 * prob.l * max_xTx);


            return Math.Pow( 2, Math.Floor(Math.Log(min_C) / Math.Log(2.0)) );
        }

        //
        // Interface functions
        //
        public Model train(Problem prob, Parameter param)
        {
            int i,j;
            int l = prob.l;
            int n = prob.n;
            int w_size = prob.n;
            Model model_ = new Model();

             if(prob.bias>=0)
                 model_.nr_feature=n-1;
             else
                 model_.nr_feature=n;
            model_.param = param;
            model_.bias = prob.bias;

            if(model_.param.check_regression_model())
            {
                model_.w = new double[w_size];
                for(i=0; i<w_size; i++)
                    model_.w[i] = 0;
                model_.nr_class = 2;
                model_.label = null;
                double[] w = model_.w;
                train_one(prob, param, ref w, 0, 0);
                model_.w = w;
            }
            else
            {
                int nr_class=0;
                int[] label = null;
                int[] start = null;
                int[] count = null;
                int[] perm = new int[l];

                // group training data of the same class
                group_classes(prob,ref nr_class, ref label, ref start, ref count, ref perm);

                model_.nr_class=nr_class;
                model_.label = new int[nr_class];
                for(i=0;i<nr_class;i++)
                    model_.label[i] = label[i];

                // calculate weighted C
                double[] weighted_C = new double[nr_class];
                for(i=0;i<nr_class;i++)
                    weighted_C[i] = param.C;
                for(i=0;i<param.nr_weight;i++)
                {
                    for(j=0;j<nr_class;j++)
                        if(param.weight_label[i] == label[j])
                            break;
                    if(j == nr_class)
                        _logger.LogError(string.Format("WARNING:ok class label {0} specified in weight is not found\n", param.weight_label[i]));
                    else
                        weighted_C[j] *= param.weight[i];
                }

                // constructing the subproblem
                int k;
                Problem sub_prob = new Problem();
                sub_prob.l = l;
                sub_prob.n = n;             
                sub_prob.y = new double[sub_prob.l];

                sub_prob.x = SparseMatrixFactory.CreateFacadeMatrix(prob.x, perm);

                // multi-class svm by Crammer and Singer
                if(param.solver_type == SOLVER_TYPE.MCSVM_CS)
                {
                    model_.w = new double[n*nr_class];
                    for(i=0;i<nr_class;i++)
                        for(j=start[i];j<start[i]+count[i];j++)
                            sub_prob.y[j] = i;
                    Solver_MCSVM_CS Solver = new Solver_MCSVM_CS(sub_prob, nr_class, weighted_C, param.eps);
                    Solver.Solve(model_.w);
                }
                else
                {
                    if(nr_class == 2)
                    {
                        model_.w = new double[w_size];

                        int e0 = start[0]+count[0];
                        k=0;
                        for(; k<e0; k++)
                            sub_prob.y[k] = +1;
                        for(; k<sub_prob.l; k++)
                            sub_prob.y[k] = -1;

                        if(param.init_sol != null)
                            for(i=0;i<w_size;i++)
                                model_.w[i] = param.init_sol[i];
                        else
                            for(i=0;i<w_size;i++)
                                model_.w[i] = 0;

                        double [] w = model_.w;
                        train_one(sub_prob, param, ref w, weighted_C[0], weighted_C[1]);
                        model_.w = w;
                    }
                    else
                    {
                        model_.w= new double[w_size*nr_class];
                        double[] w= new double[w_size];
                        for(i=0;i<nr_class;i++)
                        {
                            int si = start[i];
                            int ei = si+count[i];

                            k=0;
                            for(; k<si; k++)
                                sub_prob.y[k] = -1;
                            for(; k<ei; k++)
                                sub_prob.y[k] = +1;
                            for(; k<sub_prob.l; k++)
                                sub_prob.y[k] = -1;

                            if(param.init_sol != null)
                                for(j=0;j<w_size;j++)
                                    w[j] = param.init_sol[j*nr_class+i];
                            else
                                for(j=0;j<w_size;j++)
                                    w[j] = 0;

                            train_one(sub_prob, param, ref w, weighted_C[i], param.C);

                            for(j=0;j<w_size;j++)
                                model_.w[j*nr_class+i] = w[j];
                        }
                    }

                }
            }
            return model_;
        }  



    }
}