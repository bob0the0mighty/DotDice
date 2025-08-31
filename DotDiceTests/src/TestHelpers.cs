using System;
using System.Collections.Generic;
using DotDice.RandomNumberGenerator;

namespace DotDice.Tests
{
    /// <summary>
    /// Helper classes and utilities for testing
    /// </summary>
    public static class TestHelpers
    {
        /// <summary>
        /// Mock Random Number Generator for testing purposes
        /// </summary>
        public class MockRandomNumberGenerator : IRandomNumberGenerator<int>
        {
            private readonly List<int> _numbers;
            private int _index = 0;

            public MockRandomNumberGenerator(List<int> numbers)
            {
                _numbers = numbers;
            }

            public int Next()
            {
                return _numbers[_index++];
            }

            public int Next(int maxValue)
            {
                return _numbers[_index++];
            }

            public int Next(int minValue, int maxValue)
            {
                return _numbers[_index++];
            }

            public void SetSeed(int seed)
            {
                throw new NotImplementedException();
            }

            public int GetSeed()
            {
                throw new NotImplementedException();
            }
        }
    }
}