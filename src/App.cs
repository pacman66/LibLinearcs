using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace liblinearcs {
    public class App {
        private readonly ILogger<App> _logger;

        public App() {
            _logger = ApplicationLogging.CreateLogger<App>();
        }

        void exit_with_help(string errorMessage) {
            Console.WriteLine(
                errorMessage + "\n\n" +
                "Usage: train [options] training_set_file [model_file]\n" +
                "options:\n" +
                "-s type : set type of solver (default 1)\n" +
                "  for multi-class classification\n" +
                "	 0 -- L2-regularized logistic regression (primal)\n" +
                "	 1 -- L2-regularized L2-loss support vector classification (dual)\n" +
                "	 2 -- L2-regularized L2-loss support vector classification (primal)\n" +
                "	 3 -- L2-regularized L1-loss support vector classification (dual)\n" +
                "	 4 -- support vector classification by Crammer and Singer\n" +
                "	 5 -- L1-regularized L2-loss support vector classification\n" +
                "	 6 -- L1-regularized logistic regression\n" +
                "	 7 -- L2-regularized logistic regression (dual) for regression\n" +
                "	11 -- L2-regularized L2-loss support vector regression (primal)\n" +
                "	12 -- L2-regularized L2-loss support vector regression (dual)\n" +
                "	13 -- L2-regularized L1-loss support vector regression (dual)\n" +
                "-c cost : set the parameter C (default 1)\n" +
                "-p epsilon : set the epsilon in loss function of SVR (default 0.1)\n" +
                "-e epsilon : set tolerance of termination criterion\n" +
                "	-s 0 and 2\n" +
                "		|f'(w)|_2 <= eps*min(pos,neg)/l*|f'(w0)|_2,\n" +
                "		where f is the primal function and pos/neg are # of\n" +
                "		positive/negative data (default 0.01)\n" +
                "	-s 11\n" +
                "		|f'(w)|_2 <= eps*|f'(w0)|_2 (default 0.001)\n" +
                "	-s 1, 3, 4, and 7\n" +
                "		Dual maximal violation <= eps; similar to libsvm (default 0.1)\n" +
                "	-s 5 and 6\n" +
                "		|f'(w)|_1 <= eps*min(pos,neg)/l*|f'(w0)|_1,\n" +
                "		where f is the primal function (default 0.01)\n" +
                "	-s 12 and 13\n" +
                "		|f'(alpha)|_1 <= eps |f'(alpha0)|,\n" +
                "		where f is the dual function (default 0.1)\n" +
                "-B bias : if bias >= 0, instance x becomes [x; bias]; if < 0, no bias term added (default -1)\n" +
                "-wi weight: weights adjust the parameter C of different classes (see README for details)\n" +
                "-v n: n-fold cross validation mode\n" +
                "-C : find parameter C (only for -s 0 and 2)\n" +
                "-q : quiet mode (no outputs)\n" );
            Environment.Exit(-1);
        }


        struct commandline_parameters {

            public SOLVER_TYPE solver_type;
            public double cost; 
            public double svr_epsilon;
            public double term_epsilon;
            public double bias;
            public int nfold;
            public Dictionary<int, double> weights;
            public bool flag_find_C;
            public bool flag_quiet;
        

            // FLAGS
            public bool flag_C_specified;
            public bool flag_solver_specified;
            public bool flag_cross_validation;
            public bool flag_term_epsilon_specified;

            // FILENAMES
            public string input_file_name;
            public string model_file_name;
        };


        void do_cross_validation(Linear solver, Problem prob, Parameter param, int nFold ) {
            int i;
            int total_correct = 0;
            double total_error = 0;
            double sumv = 0, sumy = 0, sumvv = 0, sumyy = 0, sumvy = 0;
            double[] target = new double[prob.l];

            solver.cross_validation(prob, param, ref nFold, ref target);

            if( param.solver_type == SOLVER_TYPE.L2R_L2LOSS_SVR ||
                param.solver_type == SOLVER_TYPE.L2R_L1LOSS_SVR_DUAL ||
                param.solver_type == SOLVER_TYPE.L2R_L2LOSS_SVR_DUAL) {
                for(i=0;i<prob.l;i++)
                {
                    double y = prob.y[i];
                    double v = target[i];
                    total_error += (v-y)*(v-y);
                    sumv += v;
                    sumy += y;
                    sumvv += v*v;
                    sumyy += y*y;
                    sumvy += v*y;
                }
                _logger.LogInformation("Cross Validation Mean squared error = {0}", total_error / prob.l);
                _logger.LogInformation("Cross Validation Squared correlation coefficient = {0}",
                        ((prob.l*sumvy-sumv*sumy)*(prob.l*sumvy-sumv*sumy))/
                        ((prob.l*sumvv-sumv*sumv)*(prob.l*sumyy-sumy*sumy))
                    );
            }
            else
            {
                for(i=0;i<prob.l;i++)
                    if(target[i] == prob.y[i])
                        ++total_correct;
                _logger.LogInformation("correct: {0}", total_correct);
                _logger.LogInformation("Cross Validation Accuracy = {0}%",100.0*total_correct/prob.l);
            }
        }

        commandline_parameters parse_command_line(string[] args) 
        {
            commandline_parameters cp = new commandline_parameters();

            //default values
            cp.solver_type = SOLVER_TYPE.L2R_L2LOSS_SVC_DUAL;
            cp.cost = 1;
            cp.bias = -1;
            cp.svr_epsilon = 0.1;
            cp.flag_term_epsilon_specified = false;
            cp.term_epsilon = 0.1;
            cp.flag_cross_validation = false;
            cp.flag_C_specified = false;
            cp.flag_solver_specified = false;
            cp.flag_find_C = false;
            cp.nfold = 0;
            cp.weights = new Dictionary<int, double>();
            // parse options
            int i;
            for(i=0; i<args.Length; i++) {
                if (args[i][0] != '-') break;

                if(++i>=args.Length)
                    exit_with_help("Insufficient arguments");  

                switch(args[i-1][1])
                {
                    case 's':
                        int value = int.Parse(args[i]);
                        if (Enum.IsDefined(typeof(SOLVER_TYPE), value))
                                {
                                    cp.flag_solver_specified = true;
                                    cp.solver_type = (SOLVER_TYPE)value;
                                }
                        else {
                            exit_with_help(string.Format("unknown solver_type {0}", args[i]));
                        }
                        break;

                    case 'c':
                        cp.cost = double.Parse(args[i]);
                        cp.flag_C_specified = true;
                        break;

                    case 'p':
                        cp.svr_epsilon = double.Parse(args[i]);
                        break;

                    case 'e':
                        cp.term_epsilon = double.Parse(args[i]);
                        cp.flag_term_epsilon_specified = true;
                        break;

                    case 'B':
                        cp.bias = double.Parse(args[i]);
                        break;

                    case 'w':
                        cp.weights.Add(int.Parse(args[i-1][2].ToString()), double.Parse(args[i]));
                        break;

                    case 'v':
                        cp.flag_cross_validation = true;
                        cp.nfold = int.Parse(args[i]);
                        if(cp.nfold < 2)
                        {
                            exit_with_help("n-fold cross validation: n must >= 2\n");
                        }
                        break;

                    case 'q':
                        cp.flag_quiet=true;
                        i--;
                        break;

                    case 'C':
                        cp.flag_find_C = true;
                        i--;
                        break;

                    default:
                        exit_with_help(String.Format("unknown option: -{0}\n", args[i-1][1]));
                        break;
                }
            }

           // determine filenames
            if(i>=args.Length) {
                exit_with_help("Missing training_set_file name");
            }

            cp.input_file_name = args[i];

            if(i<args.Length-1)
                cp.model_file_name = args[i+1];
            else
            {
                string p;
                int index = args[i].LastIndexOf('/');
                if (index < 0) p = args[i];
                else {
                    p = args[i].Substring(index)+".model";
                }
            }
_logger.LogCritical("Solver {0}", cp.solver_type.ToString("g"));
            // default solver for parameter selection is L2R_L2LOSS_SVC
            if(cp.flag_find_C)
            {
                if(!cp.flag_cross_validation)
                    cp.nfold = 5;
                if(!cp.flag_solver_specified)
                {
                    _logger.LogInformation("Solver not specified. Using -s 2\n");
                    cp.solver_type = SOLVER_TYPE.L2R_L2LOSS_SVC;
                }
                else if(cp.solver_type != SOLVER_TYPE.L2R_LR && cp.solver_type != SOLVER_TYPE.L2R_L2LOSS_SVC)
                {
                    exit_with_help("Warm-start parameter search only available for -s 0 and -s 2\n");
                }

            }
        _logger.LogCritical("Solver {0}", Enum.GetName(typeof(SOLVER_TYPE), cp.solver_type));
            if(!cp.flag_term_epsilon_specified)
            {
                switch(cp.solver_type)
                {
                    case SOLVER_TYPE.L2R_LR:
                    case SOLVER_TYPE.L2R_L2LOSS_SVC:
                        cp.term_epsilon = 0.01;
                        break;
                    case SOLVER_TYPE.L2R_L2LOSS_SVR:
                        cp.term_epsilon = 0.001;
                        break;
                    case SOLVER_TYPE.L2R_L2LOSS_SVC_DUAL:
                    case SOLVER_TYPE.L2R_L1LOSS_SVC_DUAL:
                    case SOLVER_TYPE.MCSVM_CS:
                    case SOLVER_TYPE.L2R_LR_DUAL:
                        cp.term_epsilon = 0.1;
                        break;
                    case SOLVER_TYPE.L1R_L2LOSS_SVC:
                    case SOLVER_TYPE.L1R_LR:
                        cp.term_epsilon = 0.01;
                        break;
                    case SOLVER_TYPE.L2R_L1LOSS_SVR_DUAL:
                    case SOLVER_TYPE.L2R_L2LOSS_SVR_DUAL:
                        cp.term_epsilon = 0.1;
                        break;
                }
            }
            return cp;
        }
        
      //dotnet run -- -s 1 -c 2 -e 0.1 -v 5 E:\temp\rcv1_train.binary
        public void Run(string[] args)
        {

            // var config = new NLog.Config.LoggingConfiguration();

            // var logfile = new NLog.Targets.FileTarget("logfile") { FileName = "file.txt" };
            // var logconsole = new NLog.Targets.ConsoleTarget("logconsole");
                        
            // config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, logconsole);
            // config.AddRule(NLog.LogLevel.Debug, NLog.LogLevel.Fatal, logfile);
                        
            // NLog.LogManager.Configuration = config;

            var watch = System.Diagnostics.Stopwatch.StartNew();
            
            if (args.Length ==0)
                exit_with_help("Insufficient arguments provided");
            Loader l = new Loader();
            Model model_;
            Problem prob;
            Parameter param;
            Linear solver = new Linear();
           //Boolean flag_C_specified = false;
        	commandline_parameters p =  parse_command_line(args);

            prob = l.read_problem(p.input_file_name, p.bias);
            param = new Parameter();
            param.init_sol=null;
            param.eps = p.term_epsilon;
            param.solver_type = p.solver_type;
            param.C = p.cost;
            param.p=p.svr_epsilon;
            param.nr_weight = p.weights.Count;
            if (p.weights.Count>0) {
                param.weight = p.weights.Values.ToArray();
                param.weight_label = p.weights.Keys.ToArray();
            }
            else {
                param.weight = null;
                param.weight_label = null;
            }
            var elapsedMsMid = watch.ElapsedMilliseconds;
             _logger.LogInformation("Elpased Time {0}ms", elapsedMsMid);

	        string error_msg = param.check_parameter();

            if(error_msg != null)
            {
                _logger.LogError("ERROR: {0}\n",error_msg);
                Environment.Exit( -1 );
            }

            if (p.flag_find_C)
            {
                double start_C, best_C=0, best_rate=0;
                double max_C = 1024;
                if (p.flag_C_specified)
                    start_C = param.C;
                else
                    start_C = -1.0;
                _logger.LogInformation(String.Format("Doing parameter search with {0}-fold cross validation.", p.nfold));
                solver.find_parameter_C(prob, param, p.nfold, start_C, max_C, ref best_C, ref best_rate);
                _logger.LogInformation(String.Format("Best C = {0}  CV accuracy = {1}%", best_C, 100.0*best_rate));
            }
            else if(p.flag_cross_validation)
            {
                do_cross_validation(solver, prob, param, p.nfold);
            }
            else {
                model_=solver.train(prob, param);
                if(model_.save(p.model_file_name)==0) {
                    _logger.LogInformation(String.Format("can't save model to file {0}",p.model_file_name));
                    Environment.Exit( -1 );
                }
            }
            // the code that you want to measure comes here
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            
            _logger.LogInformation("Elapsed Time {0}ms", elapsedMs);
           
            System.Threading.Thread.Sleep(new TimeSpan(0, 0, 10));
	        //Environment.Exit( 0 );
        }

    }
}
