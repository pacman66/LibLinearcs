using System;
using System.Threading;

namespace liblinear {
    public class RandomNumberGenerator {

        //0 for false, 1 for true.
        private Random rand = null;
        
        [ThreadStatic]
        private static RandomNumberGenerator _gen = null;

        private static String genlock = "St";

        public static RandomNumberGenerator Create(int seed)
        {
            if (_gen == null) lock(genlock) _gen = new RandomNumberGenerator(seed);

            return _gen;
        }
        private RandomNumberGenerator(int seed) {
             rand = new Random(seed);
        }

        public int Next(int range) {
            return rand.Next(range);
        }


    }

}