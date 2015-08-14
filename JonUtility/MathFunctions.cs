namespace JonUtility
{
    using System;
    using System.Numerics;

    public static class MathFunctions
    {
        public enum BirthdayStyle
        {
            Exact = 0,
            Approximation1 = 1
        }

        public static double EventsUntilCollisionByProbability(double possibleOutcomes, double probability)
        {
            double logPart = 1d / (1d - probability);
            double logProb = Math.Log(logPart);
            double part = 2d * possibleOutcomes * logProb;
            double sqrt = Math.Sqrt(part);
            return sqrt;
        }

        public static double BirthdayParadox(BigInteger x, long n, BirthdayStyle style = BirthdayStyle.Exact)
        {
            switch (style)
            {
                case BirthdayStyle.Exact:
                    BigInteger xFactorial = MathFunctions.Factorial(x);
                    BigInteger xminusnFactorial = MathFunctions.Factorial(x - n);
                    BigInteger xPowN = BigInteger.Pow(x, (int)n);
                    BigInteger div1 = BigInteger.Divide(xFactorial, xminusnFactorial);
                    BigInteger div2 = BigInteger.Divide(div1 * 10000, xPowN);
                    double final = (double)div2 / 10000d;
                    return (1d - final);
                case BirthdayStyle.Approximation1:
                    double negativeNSquared = -1d * (n * (n - 1d));
                    double xTimes2 = (double)x * 2d;
                    double exponent = negativeNSquared / xTimes2;
                    double eToExponent = Math.Exp(exponent);
                    return 1 - eToExponent;
                default:
                    return -1;
            }
        }

        public static BigInteger Factorial(BigInteger number)
        {
            if (number < 0)
            {
                throw new ArgumentException("Number must be positive.");
            }
            if (number == 0 || number == 1)
            {
                return new BigInteger(1);
            }
            BigInteger bi = new BigInteger(1);
            BigInteger count = number + 1;
            for (BigInteger i = 2; i < count; i++)
            {
                BigInteger temp = i;
                bi = BigInteger.Multiply(bi, temp);
            }
            return bi;
        }
    }
}
