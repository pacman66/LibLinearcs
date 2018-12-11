using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace liblinearcs {

    public class Loader {

        private ILogger<Loader> _logger = null;
        public Loader () {
            _logger = ApplicationLogging.CreateLogger<Loader> ();
        }

        public Model load_model (string model_file_name) {

            Model model_ = null;

            try {
                StreamReader fp = new StreamReader (model_file_name);
                model_ = Model.load_model (fp);
                fp.Close ();
            } catch (IOException e) {
                _logger.LogDebug (e.Message);
                return null;
            }

            return model_;
        }

        // read in a problem (in libsvm format)
        public Problem read_problem (string filename, double bias) {
            Problem p = new Problem ();
            p.bias = bias;

            try {
                _logger.LogInformation ("Opening File");
                StreamReader fp = new StreamReader (filename);
                p = Problem.read_problem (fp, bias, _logger);
                fp.Close ();
            } catch (Exception e) {
                _logger.LogError (e.Message);
                throw e;
            }
            return p;
        }
    }
}