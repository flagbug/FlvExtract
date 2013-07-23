namespace FlvExtract
{
    internal struct FractionUInt32
    {
        public uint D;
        public uint N;

        public FractionUInt32(uint n, uint d)
        {
            N = n;
            D = d;
        }

        public void Reduce()
        {
            uint gcd = GCD(N, D);
            N /= gcd;
            D /= gcd;
        }

        public double ToDouble()
        {
            return (double)N / D;
        }

        public override string ToString()
        {
            return ToString(true);
        }

        public string ToString(bool full)
        {
            return full ? ToDouble() + " (" + N + "/" + D + ")" : ToDouble().ToString("0.####");
        }

        private static uint GCD(uint a, uint b)
        {
            while (b != 0)
            {
                uint r = a % b;
                a = b;
                b = r;
            }

            return a;
        }
    }
}