using System;
using System.Collections.Generic;
//using System.Linq;
//using System.Runtime.Serialization;
using System.Runtime.ConstrainedExecution;
//using System.Security;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace liblinearcs {
        public struct SparseRowValue : IComparable {
            public int index;

            public double value;

            public SparseRowValue(int index, double value) {
                this.value = value;
                this.index = index;
            }

            public void set(int index, double value) {
                this.value = value;
                this.index = index;
            }

            public int CompareTo(Object node) {
               // Console.WriteLine("Comparing {0} and {1}", this.index, node);
                return Math.Sign(this.index - (int)node);
            }

            public string toString() {
                return "SparseRowValue(idx=" + index + ", value=" + value + ")";
            }
        }
}