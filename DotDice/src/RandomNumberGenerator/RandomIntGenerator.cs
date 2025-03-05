using System;
using System.Numerics;

namespace DotDice.RandomNumberGenerator
{
    public class RandomIntGenerator : IRandomNumberGenerator<int>
    {
        private Random _random;
        private int _seed;
        private bool _seedInitialized = false;

        public RandomIntGenerator()
        {
            _random = new Random();
        }

        public RandomIntGenerator(int seed)
        {
            SetSeed(seed);
        }

        public void SetSeed(int seed)
        {
            _seed = seed;
            _seedInitialized = true;
            _random = new Random(seed);
        }

        public int GetSeed()
        {
            if (!_seedInitialized)
            {
                throw new InvalidOperationException("Seed was not explicitly set");
            }
            return _seed;
        }

        public int Next()
        {
            return _random.Next();
        }

        public int Next(int maxValue)
        {
            return _random.Next(maxValue);
        }

        public int Next(int minValue, int maxValue)
        {
            return _random.Next(minValue, maxValue);
        }
    }
}