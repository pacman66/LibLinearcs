using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace liblinear {
    public struct Problem {
        /// so problem is expressed as an lxn sparse matrix
        ///  The problem will have lx(n+1) with last element on each row beign a bias
        /// l the number of problem entries (lines in a libsvm file)
        public int l;

        // n the number of features (largest index in the matrix)
        public int n;

        /// The labels ( for training data in classification problems = classification of the line)
        public double[] y;

        /// A sparse Matrix format.  (In cpp the second dimansion is a pointer into an allocated space so this is not a sqaure matrix)
        //public feature_node[][] x;
        public SparseMatrix x;

        // additional feature added if bias >=0 for every line with highest index.
        public double bias;

        // read in a problem (in libsvm format)
        public static Problem read_problem (StreamReader fp, double bias, ILogger _logger) {
            _logger.LogInformation ("Starting ");
            char[] delimiters = { ' ', '\t' };
            int elements = 0;
            Problem p = new Problem ();
            p.bias = bias;
            int maxCol = 0;

            try {
                _logger.LogInformation ("Opening File");

                string line = fp.ReadLine ();

                _logger.LogInformation ("reading file phase 1");

                while (line != null) {
                    string[] tok = line.Split (delimiters);
                    elements += (String.IsNullOrWhiteSpace (tok[tok.Length - 1])) ? tok.Length - 2 : tok.Length - 1;

                    for (int i = 0; i < tok.Length; i++) {
                        if (tok[i].Contains (":")) {
                            string[] t = tok[i].Split (":");
                            maxCol = Math.Max (maxCol, int.Parse (t[0]));
                        }
                    }
                    line = fp.ReadLine ();
                    p.l++;
                    if (p.bias >= 0) elements++;
                }

                if (p.bias >= 0) {
                    maxCol++; // Attempt to add back BIAS
                    System.Console.WriteLine ("Bias {0}", p.bias);
                }

                fp.BaseStream.Position = 0;
                fp.DiscardBufferedData ();
                _logger.LogInformation ("reading file phase 2 p.l={0}, colcount={1}, element={2}", p.l, maxCol, elements);
                p.y = new double[p.l];
                p.x = SparseMatrixFactory.CreateMatrix (p.l, maxCol, elements);
                p.n = maxCol;

                // Sequential Loader mode on teh Matrix is a way to prevent some housekeeping operation as loading rows and columns in order.
                p.x.SequentialLoad = true;

                int[] cols = new int[2000];
                double[] vals = new double[2000];

                for (int i = 0; i < p.l; i++) {
                    line = fp.ReadLine ();
                    string[] tok = line.Split (delimiters);
                    int len = 0;

                    for (int j = 0; j < tok.Length; j++) {
                        string[] t = tok[j].Split (":", 2);
                        if (t.Length > 1) {
                            cols[len] = int.Parse (t[0]) - 1;
                            vals[len] = double.Parse (t[1]);
                            len++;
                        } else if (!String.IsNullOrWhiteSpace (tok[j])) {
                            p.y[i] = double.Parse (tok[j]);
                        }
                    }

                    if (p.bias >= 0) {
                        // Attempt to add back BIAS
                        cols[len] = maxCol - 1;
                        vals[len] = p.bias;
                        len++;
                    }
                    p.x.LoadRow (i, cols, vals, len);
                }

                p.x.SequentialLoad = false;

                fp.Close ();
            } catch (Exception e) {
                _logger.LogError (e.Message);
                throw new Exception ("Badly formated input");
            }
            return p;
        }
    }
}