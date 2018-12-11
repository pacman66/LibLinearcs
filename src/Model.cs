using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace liblinearcs {

    public class Model {

        private readonly ILogger<Model> _logger;

        public Model () {
            this._logger = ApplicationLogging.CreateLogger<Model> ();
        }

        private Parameter p_param;
        public Parameter param {
            get { return p_param; }
            set {
                p_param = value;
                if (value.init_sol != null) {
                    p_param.init_sol = new double[value.init_sol.Length];
                    Array.Copy (value.init_sol, p_param.init_sol, value.init_sol.Length);
                }
            }
        }

        /* number of classes */
        private int p_nr_class;
        public int nr_class {
            get { return p_nr_class; }
            set { p_nr_class = value; }
        }

        /* Number of Features */
        int p_nr_feature;
        public int nr_feature {
            get { return p_nr_feature; }
            set { p_nr_feature = value; }
        }

        /* The Bias  */
        private double p_bias;
        public double bias {
            get { return p_bias; }
            set { p_bias = value; }
        }

        private double[] p_w = null;

        public double[] w {
            get { return p_w; }
            set {
                if (p_w == null || p_w.Length != value.Length) p_w = new double[value.Length];
                Array.Copy (value, p_w, value.Length);
            }
        }

        private int[] p_label = null; /* label of each class */

        public int[] label {
            get { return p_label; }
            set {
                if (value == null) p_label = null;
                else {
                    if (p_label == null || p_label.Length != value.Length) p_label = new int[value.Length];
                    Array.Copy (value, p_label, value.Length);
                }
            }
        }

        void get_labels (ref int[] label) {
            if (label != null) {
                // Improved ensure same size
                if (label.Length < this.label.Length)
                    Helper.Clone<int> (ref label, this.p_label, nr_class);
                else
                    Array.Copy (this.label, 0, label, 0, nr_class);
            }
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public static double get_w_value (Model model_, int idx, int label_idx) {
            int nr_class = model_.nr_class;
            SOLVER_TYPE solver_type = model_.param.solver_type;
            double[] w = model_.w;

            if (idx < 0 || idx > model_.nr_feature)
                return 0;
            if (model_.param.check_regression_model ())
                return w[idx];
            else {
                if (label_idx < 0 || label_idx >= nr_class)
                    return 0;

                if (nr_class == 2 && solver_type != SOLVER_TYPE.MCSVM_CS) {
                    if (label_idx == 0)
                        return w[idx];
                    else
                        return -w[idx];
                } else
                    return w[idx * nr_class + label_idx];
            }
        }

        public int save (string model_file_name) {
            // Delete the file if it exists.
            if (File.Exists (model_file_name)) {
                File.Delete (model_file_name);
            }
            try {
                FileStream fp = File.Create (model_file_name);
                BinaryFormatter b = new BinaryFormatter ();
                b.Serialize (fp, this);
                fp.Close ();
                return 0;
            } catch (Exception e) {
                System.Diagnostics.Debug.WriteLine (e.Message);
                return -1;
            }
        }

        public static Model load (string model_file_name) {
            Model model_ = null;
            try {
                FileStream fp = File.Open (model_file_name, FileMode.Open);
                BinaryFormatter b = new BinaryFormatter ();
                model_ = (Model) b.Deserialize (fp);
                fp.Close ();
            } catch (Exception e) {
                System.Diagnostics.Debug.WriteLine (e.Message);
                return null;
            }

            return model_;
        }

        public static Model load_model (StreamReader fp) {
            Model model_ = null;
            string whitespace = "\\s+";

            try {
                model_ = new Model ();

                //model_   
                Parameter p = new Parameter ();
                p.nr_weight = 0;
                p.weight_label = null;
                p.weight = null;
                p.init_sol = null;
                model_.param = p;

                model_.label = null;

                string line = fp.ReadLine ();
                while (line != null) {
                    string[] tokens = Regex.Split (line, whitespace);

                    if (tokens[0].CompareTo ("solver_type") == 0) {

                        if (!Enum.TryParse (tokens[1], out p.solver_type)) {
                            throw new IOException ("unknown solver type");
                        }
                    } else if (line.CompareTo ("nr_class") == 0) {
                        line = fp.ReadLine ();
                        model_.nr_class = int.Parse (line);
                    } else if (tokens[0].CompareTo ("nr_feature") == 0) {
                        model_.nr_feature = int.Parse (tokens[1]);
                    } else if (tokens[0].CompareTo ("bias") == 0) {
                        model_.bias = double.Parse (tokens[1]);
                    } else if (tokens[0].CompareTo ("w") == 0) {
                        break;
                    } else if (tokens[0].CompareTo ("label") == 0) {
                        int nr_class = model_.nr_class;
                        model_.label = new int[nr_class];
                        string[] s = line.Split (" ");
                        for (int i = 0; i < nr_class; i++)
                            model_.label[i] = int.Parse (tokens[i + 1]);
                    } else {
                        throw new IOException (String.Format ("unknown text in model file: [{0}]\n", line));
                    }

                    // Load in W
                    int w_size = model_.nr_feature;
                    if (model_.bias >= 0) w_size++;

                    int nr_w = model_.nr_class;
                    if (model_.nr_class == 2 && model_.param.solver_type != SOLVER_TYPE.MCSVM_CS) nr_w = 1;

                    model_.w = new double[w_size * nr_w];
                    char[] buffer = new char[256];

                    for (int i = 0; i < w_size; i++) {
                        for (int j = 0; j < nr_w; j++) {
                            int b = 0;
                            while (true) {
                                char ch = (char) fp.Read ();
                                if (ch == -1) {
                                    throw new EndOfStreamException ("unexpected EOF");
                                }
                                if (ch == ' ') {
                                    model_.w[i * nr_w + j] = Double.Parse (new String (buffer, 0, b));
                                    break;
                                } else {
                                    if (b >= buffer.Length) {
                                        throw new ApplicationException ("illegal weight in model file at index " + (i * nr_w + j) + ", with string content '" +
                                            new String (buffer, 0, buffer.Length) + "', is not terminated " +
                                            "with a whitespace character, or is longer than expected (" + buffer.Length + " characters max).");
                                    }
                                    buffer[b++] = ch;
                                }
                            }
                        }
                    }
                }
            } catch (IOException e) {
                Console.WriteLine (e.Message);
                return null;
            }

            return model_;
        }

    }
}