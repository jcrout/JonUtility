namespace JonUtility
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;

    public static class MathFunctions
    {
        /// <summary>
        /// Compute the distance between two strings.
        /// </summary>
        public static int LevenshteinDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Step 3
            for (int i = 1; i <= n; i++)
            {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d[n, m];
        }

        public static IEnumerable<string> SortByLevenshteinDistance(string text, IEnumerable<string> strings)
        {
            var result = strings
                .Select(s => new { Text = s, Distance = LevenshteinDistance(text, s) })
                .OrderBy(s => s.Distance)
                .Select(s => s.Text);

            return result;
        }

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
