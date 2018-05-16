using System;
using System.Collections.Immutable;
using System.Linq;

namespace Mastermind
{
    public class AutosolverConfig
    {
        public AutosolverConfig(
            bool enableParallelism,
            int numThreads,
            int setSizeThreshold)
        {
            EnableParallelism = enableParallelism;
            NumThreads = numThreads;
            SetSizeThreshold = setSizeThreshold;
        }

        public bool EnableParallelism { get; }
        public int NumThreads { get; }
        public int SetSizeThreshold { get; }
    }

    public static class Autosolver
    {
        public static void Autosolve(
            AutosolverConfig config,
            Func<Code, Score> attempt)
        {
            Autosolve(config, attempt, Logic.AllCodes);
        }

        private static Code InitialGuess = new Code(Peg.Red, Peg.Red, Peg.Green, Peg.Green);

        private static void Autosolve(
            AutosolverConfig config,
            Func<Code, Score> attempt,
            IImmutableList<Code> set)
        {
            var guess =
                set.Count == Logic.AllCodes.Count ? InitialGuess :
                set.Count == 1 ? set.First() : CalculateNewGuess(config, set);

            var score = attempt(guess);

            if (score.Blacks == 4)
            {
                return;
            }

            var filteredSet = set
                .Where(code => Logic.EvaluateGuess(code, guess).Equals(score))
                .ToImmutableList();

            Autosolve(config, attempt, filteredSet);
        }

        private static Code CalculateNewGuess(
            AutosolverConfig config,
            IImmutableList<Code> set)
        {
            var best = Logic.AllCodes.AsParallel().Aggregate(
                Tuple.Create(int.MaxValue, InitialGuess),
                (currentBest, unusedCode) =>
            {
                var max = Logic.AllScores.AsParallel().Aggregate(
                    0,
                    (currentMax, score) =>
                {
                    var thisMax = set.Count(code => Logic.EvaluateGuess(unusedCode, code).Equals(score));
                    return Math.Max(currentMax, thisMax);
                });
                return (max < currentBest.Item1) ? Tuple.Create(max, unusedCode) : currentBest;
            });
            return best.Item2;
        }
    }
}
