using System.Diagnostics;

namespace liblinearcs {
    class Codetimer {
        Stopwatch watch;
        public Codetimer() {
            watch = Stopwatch.StartNew();  
            watch.Start();          
        }

        public long getTime() {
            return watch.ElapsedMilliseconds;
        }

        public void reset() {
            watch.Reset();
            watch.Start();
        }
    }
}