using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using Microsoft.Extensions.Logging;

namespace liblinear {
    public struct SparseRowValue : IComparable {
        public int index;

        public double value;

        public SparseRowValue (int index, double value) {
            this.value = value;
            this.index = index;
        }

        public void set (int index, double value) {
            this.value = value;
            this.index = index;
        }

        public int CompareTo (Object node) {
            // Console.WriteLine("Comparing {0} and {1}", this.index, node);
            return Math.Sign (this.index - (int) node);
        }

        public string toString () {
            return "SparseRowValue(idx=" + index + ", value=" + value + ")";
        }
    }
}