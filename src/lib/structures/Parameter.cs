using System;
namespace liblinear {
    public struct Parameter  {      

        public Parameter(Parameter p) {
            this = p;
            if (p.init_sol != null) {
                init_sol = new double[p.init_sol.Length];
                Array.Copy (p.init_sol, init_sol, p.init_sol.Length);
            }

            if (nr_weight > 0) {
                p.weight_label = new int[nr_weight];
                Array.Copy(p.weight_label, weight_label, nr_weight);
                p.weight = new double[nr_weight];
                Array.Copy(p.weight, weight, nr_weight);
            }
        }

        public SOLVER_TYPE solver_type;

        /* these are for training only */
        public double eps;	        /* stopping criteria */
        public double C;
        public int nr_weight;
        public int[] weight_label;
        public double[] weight;
        public double p;
        public double[] init_sol;

        public bool check_regression_model()
        {
            return (solver_type==SOLVER_TYPE.L2R_L2LOSS_SVR ||
                    solver_type==SOLVER_TYPE.L2R_L1LOSS_SVR_DUAL ||
                    solver_type==SOLVER_TYPE.L2R_L2LOSS_SVR_DUAL);
        }

        public bool check_probability_model() {
            return (solver_type==SOLVER_TYPE.L2R_LR ||
                    solver_type==SOLVER_TYPE.L2R_LR_DUAL ||
                    solver_type==SOLVER_TYPE.L1R_LR);
        }

        public string check_parameter() {
            if(eps <= 0)
                return "eps <= 0";

            if(C <= 0)
                return "C <= 0";

            if(p < 0)
                return "p < 0";

            if(init_sol != null
                && solver_type != SOLVER_TYPE.L2R_LR && solver_type != SOLVER_TYPE.L2R_L2LOSS_SVC)
                return "Initial-solution specification supported only for solver L2R_LR and L2R_L2LOSS_SVC";

            return null;
        }

    };
}