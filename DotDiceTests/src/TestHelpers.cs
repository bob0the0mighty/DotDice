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

        /// <summary>
        /// A scripted RNG that also records how it was called.
        ///
        /// <see cref="MockRandomNumberGenerator"/> returns its next scripted value no
        /// matter which overload is called or what arguments it is given, so it pins
        /// the number and order of draws but says nothing about their arguments.
        /// The evaluator's contract is stronger than that: Next(1, sides + 1) and
        /// Next(sides) + 1 cover the same range through different internal mapping in
        /// System.Random, so they carry different modulo-bias characteristics. A
        /// consumer running a Monte Carlo simulation to observe what its real RNG does
        /// only learns something if the simulated path draws randomness the way the
        /// real roll path does.
        ///
        /// Recording the arguments is what makes a reordering fail loudly instead of
        /// silently changing what a seed produces.
        /// </summary>
        public class RecordingRandomNumberGenerator : IRandomNumberGenerator<int>
        {
            private readonly IReadOnlyList<int> _numbers;
            private readonly List<string> _calls = new();
            private int _index = 0;

            public RecordingRandomNumberGenerator(IReadOnlyList<int> numbers)
            {
                if (numbers.Count == 0)
                {
                    throw new ArgumentException("script must not be empty", nameof(numbers));
                }
                _numbers = numbers;
            }

            /// <summary>Every draw so far, in order, rendered as "method(arguments)".</summary>
            public IReadOnlyList<string> Calls => _calls;

            public int Next()
            {
                _calls.Add("Next()");
                return NextScripted();
            }

            public int Next(int maxValue)
            {
                _calls.Add($"Next({maxValue})");
                return NextScripted();
            }

            public int Next(int minValue, int maxValue)
            {
                _calls.Add($"Next({minValue},{maxValue})");
                return NextScripted();
            }

            // The script cycles rather than running out, so a corpus entry whose draw
            // count depends on the values it draws (explosions, rerolls) still
            // terminates against the evaluator's own loop guards.
            private int NextScripted() => _numbers[_index++ % _numbers.Count];

            public void SetSeed(int seed)
            {
                throw new NotSupportedException("RecordingRandomNumberGenerator is scripted, not seeded");
            }

            public int GetSeed()
            {
                throw new NotSupportedException("RecordingRandomNumberGenerator is scripted, not seeded");
            }
        }

        /// <summary>
        /// A scripted RNG that cycles its values and allocates nothing while doing it.
        ///
        /// <see cref="RecordingRandomNumberGenerator"/> builds a string per draw, which
        /// is fine for pinning draw order and useless for measuring what an evaluation
        /// allocates. This one exists so an allocation assertion measures the evaluator
        /// and not its test double.
        /// </summary>
        public class CyclingRandomNumberGenerator : IRandomNumberGenerator<int>
        {
            private readonly int[] _numbers;
            private int _index = 0;

            public CyclingRandomNumberGenerator(int[] numbers)
            {
                if (numbers.Length == 0)
                {
                    throw new ArgumentException("script must not be empty", nameof(numbers));
                }
                _numbers = numbers;
            }

            public int Next() => NextScripted();

            public int Next(int maxValue) => NextScripted();

            public int Next(int minValue, int maxValue) => NextScripted();

            private int NextScripted()
            {
                var value = _numbers[_index];
                _index = _index + 1 == _numbers.Length ? 0 : _index + 1;
                return value;
            }

            public void SetSeed(int seed)
            {
                throw new NotSupportedException("CyclingRandomNumberGenerator is scripted, not seeded");
            }

            public int GetSeed()
            {
                throw new NotSupportedException("CyclingRandomNumberGenerator is scripted, not seeded");
            }
        }
    }
}