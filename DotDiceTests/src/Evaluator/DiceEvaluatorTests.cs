using System.ComponentModel;
using DotDice.Evaluator;
using DotDice.Parser;
using DotDice.RandomNumberGenerator;

namespace DotDice.Tests
{
    [TestFixture]
    public class DiceEvaluatorTests
    {
         // Mock class for testing purposes
        private record UnknownRoll : Roll { }



        // Special Random Number Generator that returns the same value repeatedly
        public class RepeatingRandomNumberGenerator : IRandomNumberGenerator<int>
        {
            private readonly int _valueToRepeat;

            public RepeatingRandomNumberGenerator(int valueToRepeat)
            {
                _valueToRepeat = valueToRepeat;
            }

            public int Next()
            {
                return _valueToRepeat;
            }

            public int Next(int maxValue)
            {
                return _valueToRepeat < maxValue ? _valueToRepeat : maxValue - 1;
            }

            public int Next(int minValue, int maxValue)
            {
                return _valueToRepeat < maxValue && _valueToRepeat >= minValue ? _valueToRepeat : minValue;
            }

            public void SetSeed(int seed)
            {
                // No-op
            }

            public int GetSeed()
            {
                throw new NotImplementedException();
            }
        }

        // Special Random Number Generator that works with multiple rolls
        public class MultiRollRandomNumberGenerator : IRandomNumberGenerator<int>
        {
            private readonly List<int> _numbers;
            private int _index = 0;

            public MultiRollRandomNumberGenerator(List<int> numbers)
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

            public void SetSeed(int seed) { /* No-op for testing */ }

            public int GetSeed()
            {
                throw new NotImplementedException();
            }
        }

        [Theory]
        [TestCaseSource(nameof(Evaluate_ConstantRoll_ReturnsConstantValue_TestCases))]
        public void Evaluate_ConstantRoll_ReturnsConstantValue(int constantValue)
        {
            var evaluator = new DiceEvaluator();
            var constant = new Constant(constantValue);
            Assert.That(evaluator.Evaluate(constant), Is.EqualTo(constantValue));
        }

        public static IEnumerable<TestCaseData> Evaluate_ConstantRoll_ReturnsConstantValue_TestCases()
        {
            yield return new TestCaseData(5);
            yield return new TestCaseData(0);
            yield return new TestCaseData(100);
            yield return new TestCaseData(-10);
            yield return new TestCaseData(int.MaxValue);
            yield return new TestCaseData(int.MinValue);
        }

        [Theory]
        [TestCaseSource(nameof(Evaluate_BasicRoll_ReturnsSumOfDice_TestCases))]
        public void Evaluate_BasicRoll_ReturnsSumOfDice(List<int> numbers, int expected)
        {
            var evaluator = new DiceEvaluator(new TestHelpers.MockRandomNumberGenerator(numbers));
            var basicRoll = new BasicRoll(numbers.Count, new DieType.Basic(6), new List<Modifier>());
            Assert.That(evaluator.Evaluate(basicRoll), Is.EqualTo(expected));
        }

        public static IEnumerable<TestCaseData> Evaluate_BasicRoll_ReturnsSumOfDice_TestCases()
        {
            yield return new TestCaseData(new List<int> { 5, 2 }, 7);
            yield return new TestCaseData(new List<int> { 10, 5, 2 }, 17);
        }

        [Theory]
        [TestCaseSource(nameof(Evaluate_PercentRoll_ReturnsSumOfDice_TestCases))]
        public void Evaluate_PercentRoll_ReturnsSumOfDice(List<int> numbers, int numberOfDice, int expected)
        {
            var evaluator = new DiceEvaluator(new TestHelpers.MockRandomNumberGenerator(numbers));
            var basicRoll = new BasicRoll(numberOfDice, new DieType.Percent(), new List<Modifier>());
            Assert.AreEqual(expected, evaluator.Evaluate(basicRoll));
        }

        public static IEnumerable<TestCaseData> Evaluate_PercentRoll_ReturnsSumOfDice_TestCases()
        {
            yield return new TestCaseData(new List<int> { 50, 75 }, 2, 125);
            yield return new TestCaseData(new List<int> { 100 }, 1, 100);
            yield return new TestCaseData(new List<int> { 1, 1, 1 }, 3, 3);
            yield return new TestCaseData(new List<int> { 33, 67, 50, 25 }, 4, 175);
            yield return new TestCaseData(new List<int> { 1, 100 }, 2, 101);
        }

        [Theory]
        [TestCaseSource(nameof(Evaluate_FudgeRoll_ReturnsSumOfDice_TestCases))]
        public void Evaluate_FudgeRoll_ReturnsSumOfDice(List<int> numbers, int numberOfDice, int expected)
        {
            var evaluator = new DiceEvaluator(new TestHelpers.MockRandomNumberGenerator(numbers));
            var basicRoll = new BasicRoll(numberOfDice, new DieType.Fudge(), new List<Modifier>());
            Assert.AreEqual(expected, evaluator.Evaluate(basicRoll));
        }

        public static IEnumerable<TestCaseData> Evaluate_FudgeRoll_ReturnsSumOfDice_TestCases()
        {
            yield return new TestCaseData(new List<int> { -1, 0, 1 }, 3, 0);
            yield return new TestCaseData(new List<int> { 1, 0, 1 }, 3, 2);
            yield return new TestCaseData(new List<int> { -1, -1, -1, -1 }, 4, -4);
            yield return new TestCaseData(new List<int> { 1, 1, 1, 1 }, 4, 4);
            yield return new TestCaseData(new List<int> { 0, 0, 0, 0, 0 }, 5, 0);
            yield return new TestCaseData(new List<int> { 1, -1, 1, -1 }, 4, 0);
        }

        [Theory]
        [TestCaseSource(nameof(Evaluate_BasicRoll_WithKeepHighestModifier_TestCases))]
        public void Evaluate_BasicRoll_WithKeepHighestModifier(List<int> numbers, int numberOfDice, DieType dieType, int keepCount, int expected)
        {
            var evaluator = new DiceEvaluator(new TestHelpers.MockRandomNumberGenerator(numbers));
            var modifiers = new List<Modifier> { new KeepModifier(keepCount, true) };
            var basicRoll = new BasicRoll(numberOfDice, dieType, modifiers);
            Assert.AreEqual(expected, evaluator.Evaluate(basicRoll));
        }

        public static IEnumerable<TestCaseData> Evaluate_BasicRoll_WithKeepHighestModifier_TestCases()
        {
            // Regular d6 cases
            yield return new TestCaseData(new List<int> { 5, 2 }, 2, new DieType.Basic(6), 1, 5);
            yield return new TestCaseData(new List<int> { 6, 4, 1 }, 3, new DieType.Basic(6), 2, 10);
            // Different dice types
            yield return new TestCaseData(new List<int> { 20, 15, 10, 5 }, 4, new DieType.Basic(20), 2, 35);
            yield return new TestCaseData(new List<int> { 10, 8, 6, 4, 2 }, 5, new DieType.Basic(10), 3, 24);
            yield return new TestCaseData(new List<int> { 4, 3, 2, 1 }, 4, new DieType.Basic(4), 1, 4);
            // Keep all dice
            yield return new TestCaseData(new List<int> { 8, 6, 4 }, 3, new DieType.Basic(8), 3, 18);
            // Fudge dice
            yield return new TestCaseData(new List<int> { 1, 0, -1, 1 }, 4, new DieType.Fudge(), 2, 2);
            // Percent dice
            yield return new TestCaseData(new List<int> { 95, 50, 33 }, 3, new DieType.Percent(), 1, 95);
            // Edge case: keep more dice than rolled
            yield return new TestCaseData(new List<int> { 5, 3 }, 2, new DieType.Basic(6), 5, 8);
            // Keep highest from mixed values
            yield return new TestCaseData(new List<int> { 2, 6, 1, 6, 3 }, 5, new DieType.Basic(6), 2, 12);
        }

        [Theory]
        [TestCaseSource(nameof(Evaluate_BasicRoll_WithKeepLowestModifier_TestCases))]
        public void Evaluate_BasicRoll_WithKeepLowestModifier(List<int> numbers, int numberOfDice, DieType dieType, int keepCount, int expected)
        {
            var evaluator = new DiceEvaluator(new TestHelpers.MockRandomNumberGenerator(numbers));
            var modifiers = new List<Modifier> { new KeepModifier(keepCount, false) };
            var basicRoll = new BasicRoll(numberOfDice, dieType, modifiers);
            Assert.AreEqual(expected, evaluator.Evaluate(basicRoll));
        }

        public static IEnumerable<TestCaseData> Evaluate_BasicRoll_WithKeepLowestModifier_TestCases()
        {
            // Regular d6 cases
            yield return new TestCaseData(new List<int> { 5, 2 }, 2, new DieType.Basic(6), 1, 2);
            yield return new TestCaseData(new List<int> { 6, 4, 1 }, 3, new DieType.Basic(6), 2, 5);
            // Different dice types
            yield return new TestCaseData(new List<int> { 20, 15, 10, 5 }, 4, new DieType.Basic(20), 2, 15);
            yield return new TestCaseData(new List<int> { 10, 8, 6, 4, 2 }, 5, new DieType.Basic(10), 3, 12);
            yield return new TestCaseData(new List<int> { 4, 3, 2, 1 }, 4, new DieType.Basic(4), 1, 1);
            // Keep all dice
            yield return new TestCaseData(new List<int> { 8, 6, 4 }, 3, new DieType.Basic(8), 3, 18);
            // Fudge dice
            yield return new TestCaseData(new List<int> { 1, 0, -1, 1 }, 4, new DieType.Fudge(), 2, -1);
            // Percent dice
            yield return new TestCaseData(new List<int> { 95, 50, 33 }, 3, new DieType.Percent(), 1, 33);
            // Edge case: keep more dice than rolled
            yield return new TestCaseData(new List<int> { 5, 3 }, 2, new DieType.Basic(6), 5, 8);
            // Keep lowest from mixed values
            yield return new TestCaseData(new List<int> { 2, 6, 1, 6, 3 }, 5, new DieType.Basic(6), 2, 3);
        }

        [Theory]
        [TestCaseSource(nameof(Evaluate_BasicRoll_WithDropHighestModifier_TestCases))]
        public void Evaluate_BasicRoll_WithDropHighestModifier(List<int> numbers, int numberOfDice, DieType dieType, int dropCount, int expected)
        {
            var evaluator = new DiceEvaluator(new TestHelpers.MockRandomNumberGenerator(numbers));
            var modifiers = new List<Modifier> { new DropModifier(dropCount, true) };
            var basicRoll = new BasicRoll(numberOfDice, dieType, modifiers);
            Assert.AreEqual(expected, evaluator.Evaluate(basicRoll));
        }

        public static IEnumerable<TestCaseData> Evaluate_BasicRoll_WithDropHighestModifier_TestCases()
        {
            // Regular d6 cases
            yield return new TestCaseData(new List<int> { 5, 2 }, 2, new DieType.Basic(6), 1, 2);
            yield return new TestCaseData(new List<int> { 6, 4, 1 }, 3, new DieType.Basic(6), 1, 5);
            // Different dice types
            yield return new TestCaseData(new List<int> { 20, 15, 10, 5 }, 4, new DieType.Basic(20), 2, 15);
            yield return new TestCaseData(new List<int> { 10, 8, 6, 4, 2 }, 5, new DieType.Basic(10), 2, 12);
            yield return new TestCaseData(new List<int> { 4, 3, 2, 1 }, 4, new DieType.Basic(4), 1, 6);
            // Drop all dice
            yield return new TestCaseData(new List<int> { 8, 6, 4 }, 3, new DieType.Basic(8), 3, 0);
            // Fudge dice
            yield return new TestCaseData(new List<int> { 1, 0, -1, 1 }, 4, new DieType.Fudge(), 2, -1);
            // Percent dice
            yield return new TestCaseData(new List<int> { 95, 50, 33 }, 3, new DieType.Percent(), 1, 83);
            // Edge case: drop more dice than rolled
            yield return new TestCaseData(new List<int> { 5, 3 }, 2, new DieType.Basic(6), 5, 0);
            // Drop highest from mixed values
            yield return new TestCaseData(new List<int> { 2, 6, 1, 6, 3 }, 5, new DieType.Basic(6), 2, 6);
        }

        [Theory]
        [TestCaseSource(nameof(Evaluate_BasicRoll_WithDropLowestModifier_TestCases))]
        public void Evaluate_BasicRoll_WithDropLowestModifier(List<int> numbers, int numberOfDice, DieType dieType, int dropCount, int expected)
        {
            var evaluator = new DiceEvaluator(new TestHelpers.MockRandomNumberGenerator(numbers));
            var modifiers = new List<Modifier> { new DropModifier(dropCount, false) };
            var basicRoll = new BasicRoll(numberOfDice, dieType, modifiers);
            Assert.AreEqual(expected, evaluator.Evaluate(basicRoll));
        }

        public static IEnumerable<TestCaseData> Evaluate_BasicRoll_WithDropLowestModifier_TestCases()
        {
            // Regular d6 cases
            yield return new TestCaseData(new List<int> { 5, 2 }, 2, new DieType.Basic(6), 1, 5);
            yield return new TestCaseData(new List<int> { 6, 4, 1 }, 3, new DieType.Basic(6), 1, 10);
            // Different dice types
            yield return new TestCaseData(new List<int> { 20, 15, 10, 5 }, 4, new DieType.Basic(20), 2, 35);
            yield return new TestCaseData(new List<int> { 10, 8, 6, 4, 2 }, 5, new DieType.Basic(10), 2, 24);
            yield return new TestCaseData(new List<int> { 4, 3, 2, 1 }, 4, new DieType.Basic(4), 1, 9);
            // Drop all dice
            yield return new TestCaseData(new List<int> { 8, 6, 4 }, 3, new DieType.Basic(8), 3, 0);
            // Fudge dice
            yield return new TestCaseData(new List<int> { 1, 0, -1, 1 }, 4, new DieType.Fudge(), 2, 2);
            // Percent dice
            yield return new TestCaseData(new List<int> { 95, 50, 33 }, 3, new DieType.Percent(), 1, 145);
            // Edge case: drop more dice than rolled
            yield return new TestCaseData(new List<int> { 5, 3 }, 2, new DieType.Basic(6), 5, 0);
            // Drop lowest from mixed values
            yield return new TestCaseData(new List<int> { 2, 6, 1, 6, 3 }, 5, new DieType.Basic(6), 2, 15);
        }

        [Theory]
        [TestCaseSource(nameof(Evaluate_BasicRoll_WithKeepHighest_DuplicateValues_TestCases))]
        public void Evaluate_BasicRoll_WithKeepHighest_DuplicateValues(List<int> numbers, int numberOfDice, int keepCount, int expected, string description)
        {
            var evaluator = new DiceEvaluator(new TestHelpers.MockRandomNumberGenerator(numbers));
            var modifiers = new List<Modifier> { new KeepModifier(keepCount, true) };
            var basicRoll = new BasicRoll(numberOfDice, new DieType.Basic(12), modifiers);
            Assert.AreEqual(expected, evaluator.Evaluate(basicRoll), description);
        }

        public static IEnumerable<TestCaseData> Evaluate_BasicRoll_WithKeepHighest_DuplicateValues_TestCases()
        {
            // 3 dice with 2 maximum values
            yield return new TestCaseData(new List<int> { 12, 12, 1 }, 3, 2, 24, "kh2: [12,12,1] should keep both 12s = 24");
            yield return new TestCaseData(new List<int> { 12, 1, 12 }, 3, 2, 24, "kh2: [12,1,12] should keep both 12s = 24");
            yield return new TestCaseData(new List<int> { 1, 12, 12 }, 3, 2, 24, "kh2: [1,12,12] should keep both 12s = 24");
            
            // 3 dice with 2 minimum values
            yield return new TestCaseData(new List<int> { 1, 1, 12 }, 3, 2, 13, "kh2: [1,1,12] should keep one 1 and 12 = 13");
            yield return new TestCaseData(new List<int> { 1, 12, 1 }, 3, 2, 13, "kh2: [1,12,1] should keep one 1 and 12 = 13");
            yield return new TestCaseData(new List<int> { 12, 1, 1 }, 3, 2, 13, "kh2: [12,1,1] should keep one 1 and 12 = 13");
            
            // 4 dice with 2 maximums and 2 minimums
            yield return new TestCaseData(new List<int> { 12, 12, 1, 1 }, 4, 2, 24, "kh2: [12,12,1,1] should keep both 12s = 24");
            yield return new TestCaseData(new List<int> { 12, 1, 12, 1 }, 4, 2, 24, "kh2: [12,1,12,1] should keep both 12s = 24");
            yield return new TestCaseData(new List<int> { 1, 12, 1, 12 }, 4, 2, 24, "kh2: [1,12,1,12] should keep both 12s = 24");
            
            // 4 dice with interspersed values
            yield return new TestCaseData(new List<int> { 12, 6, 12, 1 }, 4, 2, 24, "kh2: [12,6,12,1] should keep both 12s = 24");
            yield return new TestCaseData(new List<int> { 12, 6, 6, 1 }, 4, 2, 18, "kh2: [12,6,6,1] should keep 12 and one 6 = 18");
            yield return new TestCaseData(new List<int> { 6, 6, 1, 1 }, 4, 2, 12, "kh2: [6,6,1,1] should keep both 6s = 12");
            
            // 5 dice with various combinations
            yield return new TestCaseData(new List<int> { 12, 12, 12, 1, 1 }, 5, 3, 36, "kh3: [12,12,12,1,1] should keep all three 12s = 36");
            yield return new TestCaseData(new List<int> { 12, 12, 6, 1, 1 }, 5, 3, 30, "kh3: [12,12,6,1,1] should keep both 12s and 6 = 30");
            yield return new TestCaseData(new List<int> { 12, 6, 6, 6, 1 }, 5, 3, 24, "kh3: [12,6,6,6,1] should keep 12 and two 6s = 24");
            yield return new TestCaseData(new List<int> { 6, 6, 6, 1, 1 }, 5, 3, 18, "kh3: [6,6,6,1,1] should keep all three 6s = 18");
            yield return new TestCaseData(new List<int> { 12, 10, 8, 6, 4 }, 5, 3, 30, "kh3: [12,10,8,6,4] should keep 12,10,8 = 30");
            
            // Edge case: keep all when duplicates exist
            yield return new TestCaseData(new List<int> { 6, 6, 6 }, 3, 3, 18, "kh3: [6,6,6] should keep all = 18");
            yield return new TestCaseData(new List<int> { 1, 1, 2 }, 3, 2, 3, "kh2: [1,1,2] should keep one 1 and 2 = 3");
        }

        [Theory]
        [TestCaseSource(nameof(Evaluate_BasicRoll_WithKeepLowest_DuplicateValues_TestCases))]
        public void Evaluate_BasicRoll_WithKeepLowest_DuplicateValues(List<int> numbers, int numberOfDice, int keepCount, int expected, string description)
        {
            var evaluator = new DiceEvaluator(new TestHelpers.MockRandomNumberGenerator(numbers));
            var modifiers = new List<Modifier> { new KeepModifier(keepCount, false) };
            var basicRoll = new BasicRoll(numberOfDice, new DieType.Basic(12), modifiers);
            Assert.AreEqual(expected, evaluator.Evaluate(basicRoll), description);
        }

        public static IEnumerable<TestCaseData> Evaluate_BasicRoll_WithKeepLowest_DuplicateValues_TestCases()
        {
            // 3 dice with 2 maximum values
            yield return new TestCaseData(new List<int> { 12, 12, 1 }, 3, 2, 13, "kl2: [12,12,1] should keep 1 and one 12 = 13");
            yield return new TestCaseData(new List<int> { 12, 1, 12 }, 3, 2, 13, "kl2: [12,1,12] should keep 1 and one 12 = 13");
            yield return new TestCaseData(new List<int> { 1, 12, 12 }, 3, 2, 13, "kl2: [1,12,12] should keep 1 and one 12 = 13");
            
            // 3 dice with 2 minimum values
            yield return new TestCaseData(new List<int> { 1, 1, 12 }, 3, 2, 2, "kl2: [1,1,12] should keep both 1s = 2");
            yield return new TestCaseData(new List<int> { 1, 12, 1 }, 3, 2, 2, "kl2: [1,12,1] should keep both 1s = 2");
            yield return new TestCaseData(new List<int> { 12, 1, 1 }, 3, 2, 2, "kl2: [12,1,1] should keep both 1s = 2");
            
            // 4 dice with 2 maximums and 2 minimums
            yield return new TestCaseData(new List<int> { 12, 12, 1, 1 }, 4, 2, 2, "kl2: [12,12,1,1] should keep both 1s = 2");
            yield return new TestCaseData(new List<int> { 12, 1, 12, 1 }, 4, 2, 2, "kl2: [12,1,12,1] should keep both 1s = 2");
            yield return new TestCaseData(new List<int> { 1, 12, 1, 12 }, 4, 2, 2, "kl2: [1,12,1,12] should keep both 1s = 2");
            
            // 4 dice with interspersed values
            yield return new TestCaseData(new List<int> { 12, 6, 12, 1 }, 4, 2, 7, "kl2: [12,6,12,1] should keep 1 and 6 = 7");
            yield return new TestCaseData(new List<int> { 12, 6, 6, 1 }, 4, 2, 7, "kl2: [12,6,6,1] should keep 1 and one 6 = 7");
            yield return new TestCaseData(new List<int> { 6, 6, 1, 1 }, 4, 2, 2, "kl2: [6,6,1,1] should keep both 1s = 2");
            
            // 5 dice with various combinations
            yield return new TestCaseData(new List<int> { 12, 12, 12, 1, 1 }, 5, 3, 14, "kl3: [12,12,12,1,1] should keep both 1s and one 12 = 14");
            yield return new TestCaseData(new List<int> { 12, 12, 6, 1, 1 }, 5, 3, 8, "kl3: [12,12,6,1,1] should keep both 1s and 6 = 8");
            yield return new TestCaseData(new List<int> { 12, 6, 6, 6, 1 }, 5, 3, 13, "kl3: [12,6,6,6,1] should keep 1 and two 6s = 13");
            yield return new TestCaseData(new List<int> { 6, 6, 6, 1, 1 }, 5, 3, 8, "kl3: [6,6,6,1,1] should keep both 1s and one 6 = 8");
            yield return new TestCaseData(new List<int> { 12, 10, 8, 6, 4 }, 5, 3, 18, "kl3: [12,10,8,6,4] should keep 4,6,8 = 18");
            
            // Edge case: keep all when duplicates exist
            yield return new TestCaseData(new List<int> { 6, 6, 6 }, 3, 3, 18, "kl3: [6,6,6] should keep all = 18");
            yield return new TestCaseData(new List<int> { 1, 1, 2 }, 3, 2, 2, "kl2: [1,1,2] should keep both 1s = 2");
        }

        [Theory]
        [TestCaseSource(nameof(Evaluate_BasicRoll_WithDropHighest_DuplicateValues_TestCases))]
        public void Evaluate_BasicRoll_WithDropHighest_DuplicateValues(List<int> numbers, int numberOfDice, int dropCount, int expected, string description)
        {
            var evaluator = new DiceEvaluator(new TestHelpers.MockRandomNumberGenerator(numbers));
            var modifiers = new List<Modifier> { new DropModifier(dropCount, true) };
            var basicRoll = new BasicRoll(numberOfDice, new DieType.Basic(12), modifiers);
            Assert.AreEqual(expected, evaluator.Evaluate(basicRoll), description);
        }

        public static IEnumerable<TestCaseData> Evaluate_BasicRoll_WithDropHighest_DuplicateValues_TestCases()
        {
            // 3 dice with 2 maximum values
            yield return new TestCaseData(new List<int> { 12, 12, 1 }, 3, 2, 1, "dh2: [12,12,1] should drop both 12s = 1");
            yield return new TestCaseData(new List<int> { 12, 1, 12 }, 3, 2, 1, "dh2: [12,1,12] should drop both 12s = 1");
            yield return new TestCaseData(new List<int> { 1, 12, 12 }, 3, 2, 1, "dh2: [1,12,12] should drop both 12s = 1");
            
            // 3 dice with 2 minimum values
            yield return new TestCaseData(new List<int> { 1, 1, 12 }, 3, 2, 1, "dh2: [1,1,12] should drop 12 and one 1 = 1");
            yield return new TestCaseData(new List<int> { 1, 12, 1 }, 3, 2, 1, "dh2: [1,12,1] should drop 12 and one 1 = 1");
            yield return new TestCaseData(new List<int> { 12, 1, 1 }, 3, 2, 1, "dh2: [12,1,1] should drop 12 and one 1 = 1");
            
            // 4 dice with 2 maximums and 2 minimums
            yield return new TestCaseData(new List<int> { 12, 12, 1, 1 }, 4, 2, 2, "dh2: [12,12,1,1] should drop both 12s = 2");
            yield return new TestCaseData(new List<int> { 12, 1, 12, 1 }, 4, 2, 2, "dh2: [12,1,12,1] should drop both 12s = 2");
            yield return new TestCaseData(new List<int> { 1, 12, 1, 12 }, 4, 2, 2, "dh2: [1,12,1,12] should drop both 12s = 2");
            
            // 4 dice with interspersed values
            yield return new TestCaseData(new List<int> { 12, 6, 12, 1 }, 4, 2, 7, "dh2: [12,6,12,1] should drop both 12s = 7");
            yield return new TestCaseData(new List<int> { 12, 6, 6, 1 }, 4, 2, 7, "dh2: [12,6,6,1] should drop 12 and one 6 = 7");
            yield return new TestCaseData(new List<int> { 6, 6, 1, 1 }, 4, 2, 2, "dh2: [6,6,1,1] should drop both 6s = 2");
            
            // 5 dice with various combinations
            yield return new TestCaseData(new List<int> { 12, 12, 12, 1, 1 }, 5, 3, 2, "dh3: [12,12,12,1,1] should drop all three 12s = 2");
            yield return new TestCaseData(new List<int> { 12, 12, 6, 1, 1 }, 5, 3, 2, "dh3: [12,12,6,1,1] should drop both 12s and 6 = 2");
            yield return new TestCaseData(new List<int> { 12, 6, 6, 6, 1 }, 5, 3, 7, "dh3: [12,6,6,6,1] should drop 12 and two 6s = 7");
            yield return new TestCaseData(new List<int> { 6, 6, 6, 1, 1 }, 5, 3, 2, "dh3: [6,6,6,1,1] should drop all three 6s = 2");
            yield return new TestCaseData(new List<int> { 12, 10, 8, 6, 4 }, 5, 3, 10, "dh3: [12,10,8,6,4] should drop 12,10,8 = 10");
            
            // Edge case: drop all when duplicates exist
            yield return new TestCaseData(new List<int> { 6, 6, 6 }, 3, 3, 0, "dh3: [6,6,6] should drop all = 0");
            yield return new TestCaseData(new List<int> { 1, 1, 2 }, 3, 2, 1, "dh2: [1,1,2] should drop 2 and one 1 = 1");
        }

        [Theory]
        [TestCaseSource(nameof(Evaluate_BasicRoll_WithDropLowest_DuplicateValues_TestCases))]
        public void Evaluate_BasicRoll_WithDropLowest_DuplicateValues(List<int> numbers, int numberOfDice, int dropCount, int expected, string description)
        {
            var evaluator = new DiceEvaluator(new TestHelpers.MockRandomNumberGenerator(numbers));
            var modifiers = new List<Modifier> { new DropModifier(dropCount, false) };
            var basicRoll = new BasicRoll(numberOfDice, new DieType.Basic(12), modifiers);
            Assert.AreEqual(expected, evaluator.Evaluate(basicRoll), description);
        }

        public static IEnumerable<TestCaseData> Evaluate_BasicRoll_WithDropLowest_DuplicateValues_TestCases()
        {
            // 3 dice with 2 maximum values
            yield return new TestCaseData(new List<int> { 12, 12, 1 }, 3, 2, 12, "dl2: [12,12,1] should drop 1 and one 12 = 12");
            yield return new TestCaseData(new List<int> { 12, 1, 12 }, 3, 2, 12, "dl2: [12,1,12] should drop 1 and one 12 = 12");
            yield return new TestCaseData(new List<int> { 1, 12, 12 }, 3, 2, 12, "dl2: [1,12,12] should drop 1 and one 12 = 12");
            
            // 3 dice with 2 minimum values
            yield return new TestCaseData(new List<int> { 1, 1, 12 }, 3, 2, 12, "dl2: [1,1,12] should drop both 1s = 12");
            yield return new TestCaseData(new List<int> { 1, 12, 1 }, 3, 2, 12, "dl2: [1,12,1] should drop both 1s = 12");
            yield return new TestCaseData(new List<int> { 12, 1, 1 }, 3, 2, 12, "dl2: [12,1,1] should drop both 1s = 12");
            
            // 4 dice with 2 maximums and 2 minimums
            yield return new TestCaseData(new List<int> { 12, 12, 1, 1 }, 4, 2, 24, "dl2: [12,12,1,1] should drop both 1s = 24");
            yield return new TestCaseData(new List<int> { 12, 1, 12, 1 }, 4, 2, 24, "dl2: [12,1,12,1] should drop both 1s = 24");
            yield return new TestCaseData(new List<int> { 1, 12, 1, 12 }, 4, 2, 24, "dl2: [1,12,1,12] should drop both 1s = 24");
            
            // 4 dice with interspersed values
            yield return new TestCaseData(new List<int> { 12, 6, 12, 1 }, 4, 2, 24, "dl2: [12,6,12,1] should drop 1 and 6 = 24");
            yield return new TestCaseData(new List<int> { 12, 6, 6, 1 }, 4, 2, 18, "dl2: [12,6,6,1] should drop 1 and one 6 = 18");
            yield return new TestCaseData(new List<int> { 6, 6, 1, 1 }, 4, 2, 12, "dl2: [6,6,1,1] should drop both 1s = 12");
            
            // 5 dice with various combinations
            yield return new TestCaseData(new List<int> { 12, 12, 12, 1, 1 }, 5, 3, 24, "dl3: [12,12,12,1,1] should drop both 1s and one 12 = 24");
            yield return new TestCaseData(new List<int> { 12, 12, 6, 1, 1 }, 5, 3, 24, "dl3: [12,12,6,1,1] should drop both 1s and 6 = 24");
            yield return new TestCaseData(new List<int> { 12, 6, 6, 6, 1 }, 5, 3, 18, "dl3: [12,6,6,6,1] should drop 1 and two 6s = 18");
            yield return new TestCaseData(new List<int> { 6, 6, 6, 1, 1 }, 5, 3, 12, "dl3: [6,6,6,1,1] should drop both 1s and one 6 = 12");
            yield return new TestCaseData(new List<int> { 12, 10, 8, 6, 4 }, 5, 3, 22, "dl3: [12,10,8,6,4] should drop 4,6,8 = 22");
            
            // Edge case: drop all when duplicates exist
            yield return new TestCaseData(new List<int> { 6, 6, 6 }, 3, 3, 0, "dl3: [6,6,6] should drop all = 0");
            yield return new TestCaseData(new List<int> { 1, 1, 2 }, 3, 2, 2, "dl2: [1,1,2] should drop both 1s = 2");
        }

        [Theory]
        [TestCaseSource(nameof(Evaluate_BasicRoll_WithRerollOnceModifier_TestCases))]
        public void Evaluate_BasicRoll_WithRerollOnceModifier(List<int> numbers, int numberOfDice, DieType dieType, ComparisonOperator comparisonOperator, int value, int expected)
        {
            var evaluator = new DiceEvaluator(new TestHelpers.MockRandomNumberGenerator(numbers));
            var modifiers = new List<Modifier> { new RerollOnceModifier(comparisonOperator, value) };
            var basicRoll = new BasicRoll(numberOfDice, dieType, modifiers);
            Assert.AreEqual(expected, evaluator.Evaluate(basicRoll));
        }

        public static IEnumerable<TestCaseData> Evaluate_BasicRoll_WithRerollOnceModifier_TestCases()
        {
            // Test case 1: Reroll one d4, reroll if less than 2
            yield return new TestCaseData(new List<int> { 1, 3 }, 1, new DieType.Basic(4), ComparisonOperator.LessThan, 2, 3);
            // Test case 2: Reroll two d6, reroll if equal to 3
            yield return new TestCaseData(new List<int> { 3, 5, 2, 4 }, 2, new DieType.Basic(6), ComparisonOperator.Equal, 3, 7);
            // Test case 3: Reroll three d8, reroll if greater than 6
            yield return new TestCaseData(new List<int> { 7, 4, 8, 3, 5 }, 3, new DieType.Basic(8), ComparisonOperator.GreaterThan, 6, 12);
            // Test case 4: Reroll four d10, reroll if less than or equal to 4
            yield return new TestCaseData(new List<int> { 2, 5, 4, 9, 1, 6 }, 4, new DieType.Basic(10), ComparisonOperator.LessThan, 5, 21);
            // Test case 5: Reroll five d20, reroll if greater than or equal to 15
            yield return new TestCaseData(new List<int> { 16, 8, 15, 12, 18, 5, 10, 14 }, 5, new DieType.Basic(20), ComparisonOperator.GreaterThan, 14, 49);
            // Test case 6: Reroll one d100, reroll if equal to 100
            yield return new TestCaseData(new List<int> { 100, 50 }, 1, new DieType.Basic(100), ComparisonOperator.Equal, 100, 50);
        }

        [Theory]
        [TestCaseSource(nameof(Evaluate_BasicRoll_WithRerollMultipleModifier_TestCases))]
        public void Evaluate_BasicRoll_WithRerollMultipleModifier(List<int> numbers, int numberOfDice, DieType dieType, ComparisonOperator comparisonOperator, int value, int expected)
        {
            var evaluator = new DiceEvaluator(new TestHelpers.MockRandomNumberGenerator(numbers));
            var modifiers = new List<Modifier> { new RerollMultipleModifier(comparisonOperator, value) };
            var basicRoll = new BasicRoll(numberOfDice, dieType, modifiers);
            Assert.AreEqual(expected, evaluator.Evaluate(basicRoll));
        }

        public static IEnumerable<TestCaseData> Evaluate_BasicRoll_WithRerollMultipleModifier_TestCases()
        {
            // Test case 1: Reroll one d6, reroll if less than 3
            yield return new TestCaseData(new List<int> { 1, 6 }, 1, new DieType.Basic(6), ComparisonOperator.LessThan, 3, 6);
            // Test case 2: Reroll two d4, reroll if equal to 1
            yield return new TestCaseData(new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, 1, new DieType.Basic(20), ComparisonOperator.LessThan, 10, 10);
            // Test case 3: Reroll three d8, reroll if greater than 5
            yield return new TestCaseData(new List<int> { 6, 2, 7, 1, 3 }, 3, new DieType.Basic(8), ComparisonOperator.GreaterThan, 5, 6);
            // Test case 4: Reroll two d10, reroll if less than or equal to 2 (Reroll if LessThan 3)
            yield return new TestCaseData(new List<int> { 2, 5, 3, 8 }, 2, new DieType.Basic(10), ComparisonOperator.LessThan, 4, 13);
            // Test case 5: Reroll one d100, reroll if greater than or equal to 90 (Reroll if GreaterThan 89)
            yield return new TestCaseData(new List<int> { 95, 50 }, 1, new DieType.Percent(), ComparisonOperator.GreaterThan, 89, 50);
            // Test case 6: Reroll two d6, reroll if not equal to 4 (Reroll if LessThan 4 or GreaterThan 4) - simplified to LessThan 4
            yield return new TestCaseData(new List<int> { 1, 4, 5, 2 }, 2, new DieType.Basic(6), ComparisonOperator.LessThan, 4, 9);
            // Test case 7: Reroll three d4, reroll if less than 2
            yield return new TestCaseData(new List<int> { 1, 3, 1, 4, 2, 6 }, 3, new DieType.Basic(4), ComparisonOperator.LessThan, 3, 13);
            // Test case 8: Reroll four d8, reroll if greater than or equal to 7 (Reroll if GreaterThan 6)
            yield return new TestCaseData(new List<int> { 7, 2, 8, 3, 4, 5 }, 4, new DieType.Basic(8), ComparisonOperator.GreaterThan, 6, 14);
            // Test case 9: Reroll two d10, reroll if equal to 10
            yield return new TestCaseData(new List<int> { 10, 5, 1, 6 }, 2, new DieType.Basic(10), ComparisonOperator.Equal, 10, 6);
            // Test case 10: Reroll one d20, reroll if less than or equal to 5 (Reroll if LessThan 6)
            yield return new TestCaseData(new List<int> { 3, 15 }, 1, new DieType.Basic(20), ComparisonOperator.LessThan, 6, 15);
        }

        [Theory]
        [TestCaseSource(nameof(Evaluate_BasicRoll_WithExplodeModifier_TestCases))]
        public void Evaluate_BasicRoll_WithExplodeModifier(IRandomNumberGenerator<int> rng, int numberOfDice, DieType dieType, ComparisonOperator comparisonOperator, int value, int expected)
        {
            var evaluator = new DiceEvaluator(rng);
            var modifiers = new List<Modifier> { new ExplodeModifier(comparisonOperator, value) };
            var basicRoll = new BasicRoll(numberOfDice, dieType, modifiers);
            Assert.AreEqual(expected, evaluator.Evaluate(basicRoll));
        }

        private static IEnumerable<TestCaseData> Evaluate_BasicRoll_WithExplodeModifier_TestCases()
        {
            // Single dice tests with various die types
            yield return new TestCaseData(new TestHelpers.MockRandomNumberGenerator(new List<int> { 6, 6, 2 }), 1, new DieType.Basic(6), ComparisonOperator.GreaterThan, 5, 14);
            yield return new TestCaseData(new TestHelpers.MockRandomNumberGenerator(new List<int> { 4, 4, 2 }), 1, new DieType.Basic(4), ComparisonOperator.Equal, 4, 10);
            yield return new TestCaseData(new TestHelpers.MockRandomNumberGenerator(new List<int> { 8, 7, 6 }), 1, new DieType.Basic(8), ComparisonOperator.GreaterThan, 6, 21);
            yield return new TestCaseData(new TestHelpers.MockRandomNumberGenerator(new List<int> { 10, 8 }), 1, new DieType.Basic(10), ComparisonOperator.GreaterThan, 9, 18);
            yield return new TestCaseData(new TestHelpers.MockRandomNumberGenerator(new List<int> { 20, 15 }), 1, new DieType.Basic(20), ComparisonOperator.Equal, 20, 35);
            // Multiple dice tests
            yield return new TestCaseData(new TestHelpers.MockRandomNumberGenerator(new List<int> { 5, 6, 6, 3 }), 2, new DieType.Basic(6), ComparisonOperator.GreaterThan, 5, 20);
            yield return new TestCaseData(new TestHelpers.MockRandomNumberGenerator(new List<int> { 8, 3, 8, 8, 2 }), 2, new DieType.Basic(8), ComparisonOperator.Equal, 8, 29);
            yield return new TestCaseData(new TestHelpers.MockRandomNumberGenerator(new List<int> { 4, 2, 3, 4, 4, 2 }), 3, new DieType.Basic(4), ComparisonOperator.GreaterThan, 3, 19);
            // Different die types
            yield return new TestCaseData(new TestHelpers.MockRandomNumberGenerator(new List<int> { 12, 12, 5 }), 1, new DieType.Basic(12), ComparisonOperator.GreaterThan, 11, 29);
            yield return new TestCaseData(new TestHelpers.MockRandomNumberGenerator(new List<int> { 100, 50 }), 1, new DieType.Percent(), ComparisonOperator.Equal, 100, 150);
            yield return new TestCaseData(new TestHelpers.MockRandomNumberGenerator(new List<int> { 1, 1, -1 }), 1, new DieType.Fudge(), ComparisonOperator.Equal, 1, 1);
            // Mixed scenarios
            yield return new TestCaseData(new TestHelpers.MockRandomNumberGenerator(new List<int> { 7, 9, 10, 4 }), 2, new DieType.Basic(10), ComparisonOperator.GreaterThan, 8, 30);
            yield return new TestCaseData(new TestHelpers.MockRandomNumberGenerator(new List<int> { 3, 3, 3, 2 }), 1, new DieType.Basic(3), ComparisonOperator.GreaterThan, 2, 11);
            // Max explosion tests (with repeating generator)
            yield return new TestCaseData(new RepeatingRandomNumberGenerator(6), 1, new DieType.Basic(6), ComparisonOperator.GreaterThan, 5, 606); // Will hit explosion limit of 100
            yield return new TestCaseData(new RepeatingRandomNumberGenerator(20), 1, new DieType.Basic(20), ComparisonOperator.Equal, 20, 2020); // Will hit explosion limit of 100
        }

        [Theory]
        [TestCaseSource(nameof(Evaluate_BasicRoll_WithCompoundingModifier_TestCases))]
        public void Evaluate_BasicRoll_WithCompoundingModifier(IRandomNumberGenerator<int> rng, int numberOfDice, DieType dieType, ComparisonOperator comparisonOperator, int value, int expected)
        {
            var evaluator = new DiceEvaluator(rng);
            var modifiers = new List<Modifier> { new CompoundingModifier(comparisonOperator, value) };
            var basicRoll = new BasicRoll(numberOfDice, dieType, modifiers);
            Assert.AreEqual(expected, evaluator.Evaluate(basicRoll));
        }

        public static IEnumerable<TestCaseData> Evaluate_BasicRoll_WithCompoundingModifier_TestCases()
        {
            // Single dice tests with various die types
            yield return new TestCaseData(new TestHelpers.MockRandomNumberGenerator(new List<int> { 6, 6, 2 }), 1, new DieType.Basic(6), ComparisonOperator.GreaterThan, 5, 14);
            yield return new TestCaseData(new TestHelpers.MockRandomNumberGenerator(new List<int> { 4, 4, 2 }), 1, new DieType.Basic(4), ComparisonOperator.Equal, 4, 10);
            yield return new TestCaseData(new TestHelpers.MockRandomNumberGenerator(new List<int> { 8, 7, 6 }), 1, new DieType.Basic(8), ComparisonOperator.GreaterThan, 6, 21);
            yield return new TestCaseData(new TestHelpers.MockRandomNumberGenerator(new List<int> { 10, 8 }), 1, new DieType.Basic(10), ComparisonOperator.GreaterThan, 9, 18);
            yield return new TestCaseData(new TestHelpers.MockRandomNumberGenerator(new List<int> { 20, 15 }), 1, new DieType.Basic(20), ComparisonOperator.Equal, 20, 35);
            // Multiple dice tests (each die is evaluated separately for compounding)
            yield return new TestCaseData(new TestHelpers.MockRandomNumberGenerator(new List<int> { 6, 3, 6, 4 }), 2, new DieType.Basic(6), ComparisonOperator.GreaterThan, 5, 19);
            yield return new TestCaseData(new TestHelpers.MockRandomNumberGenerator(new List<int> { 8, 5, 8, 7 }), 2, new DieType.Basic(8), ComparisonOperator.Equal, 8, 28);
            yield return new TestCaseData(new TestHelpers.MockRandomNumberGenerator(new List<int> { 4, 3, 2, 4, 4, 1 }), 3, new DieType.Basic(4), ComparisonOperator.GreaterThan, 3, 18);
            // Different die types
            yield return new TestCaseData(new TestHelpers.MockRandomNumberGenerator(new List<int> { 12, 12, 5 }), 1, new DieType.Basic(12), ComparisonOperator.GreaterThan, 11, 29);
            yield return new TestCaseData(new TestHelpers.MockRandomNumberGenerator(new List<int> { 100, 50 }), 1, new DieType.Percent(), ComparisonOperator.Equal, 100, 150);
            yield return new TestCaseData(new TestHelpers.MockRandomNumberGenerator(new List<int> { 1, 1, -1 }), 1, new DieType.Fudge(), ComparisonOperator.Equal, 1, 1);
            // Mixed scenarios
            yield return new TestCaseData(new TestHelpers.MockRandomNumberGenerator(new List<int> { 10, 10, 10, 2, 5 }), 2, new DieType.Basic(10), ComparisonOperator.GreaterThan, 9, 37);
            yield return new TestCaseData(new TestHelpers.MockRandomNumberGenerator(new List<int> { 3, 3, 3, 3, 1 }), 1, new DieType.Basic(3), ComparisonOperator.GreaterThan, 2, 13);
            // Max compound tests (with repeating generator)
            yield return new TestCaseData(new RepeatingRandomNumberGenerator(6), 1, new DieType.Basic(6), ComparisonOperator.GreaterThan, 5, 606); // Will hit compound limit of 100
            yield return new TestCaseData(new RepeatingRandomNumberGenerator(20), 1, new DieType.Basic(20), ComparisonOperator.Equal, 20, 2020); // Will hit compound limit of 100
        }

        [Theory]
        [TestCaseSource(nameof(Evaluate_BasicRoll_WithSuccessModifier_TestCases))]
        public void Evaluate_BasicRoll_WithSuccessModifier(List<int> numbers, int numberOfDice, DieType dieType, ComparisonOperator comparisonOperator, int value, int expected)
        {
            var evaluator = new DiceEvaluator(new TestHelpers.MockRandomNumberGenerator(numbers));
            var modifiers = new List<Modifier> { new SuccessModifier(comparisonOperator, value) };
            var basicRoll = new BasicRoll(numberOfDice, dieType, modifiers);
            Assert.AreEqual(expected, evaluator.Evaluate(basicRoll));
        }

        public static IEnumerable<TestCaseData> Evaluate_BasicRoll_WithSuccessModifier_TestCases()
        {
            // Test case 1: Count successes for 3d6 where success is > 4
            yield return new TestCaseData(new List<int> { 5, 2, 6 }, 3, new DieType.Basic(6), ComparisonOperator.GreaterThan, 4, 2);

            // Test case 2: Count successes for 5d10 where success is > 7
            yield return new TestCaseData(new List<int> { 8, 3, 9, 5, 2 }, 5, new DieType.Basic(10), ComparisonOperator.GreaterThan, 7, 2);

            // Test case 3: Count successes for 4d20 where success is = 20 (critical hit)
            yield return new TestCaseData(new List<int> { 20, 14, 20, 7 }, 4, new DieType.Basic(20), ComparisonOperator.Equal, 20, 2);

            // Test case 4: Count successes for 6d4 where success is > 3
            yield return new TestCaseData(new List<int> { 4, 2, 4, 1, 3, 2 }, 6, new DieType.Basic(4), ComparisonOperator.GreaterThan, 3, 2);

            // Test case 5: Count successes for 2d100 where success is < 50 (percentile check)
            yield return new TestCaseData(new List<int> { 23, 67 }, 2, new DieType.Percent(), ComparisonOperator.LessThan, 50, 1);

            // Test case 6: Count successes for 5d6 where success is = 6
            yield return new TestCaseData(new List<int> { 6, 3, 6, 2, 6 }, 5, new DieType.Basic(6), ComparisonOperator.Equal, 6, 3);

            // Test case 7: Count successes for 7d8 where success is > 6
            yield return new TestCaseData(new List<int> { 7, 8, 3, 5, 2, 7, 8 }, 7, new DieType.Basic(8), ComparisonOperator.GreaterThan, 6, 4);

            // Test case 8: Count successes for 4fudge (fudge dice) where success is > 0
            yield return new TestCaseData(new List<int> { 1, -1, 0, 1 }, 4, new DieType.Fudge(), ComparisonOperator.GreaterThan, 0, 2);

            // Test case 9: Count successes for 3d12 where success is < 4
            yield return new TestCaseData(new List<int> { 2, 7, 3 }, 3, new DieType.Basic(12), ComparisonOperator.LessThan, 4, 2);

            // Test case 10: Count successes for 10d6 where success is > 3
            yield return new TestCaseData(new List<int> { 4, 6, 2, 1, 5, 3, 6, 5, 4, 2 }, 10, new DieType.Basic(6), ComparisonOperator.GreaterThan, 3, 6);
        }

        [Theory]
        [TestCaseSource(nameof(Evaluate_BasicRoll_WithFailureModifier_TestCases))]
        public void Evaluate_BasicRoll_WithFailureModifier(List<int> numbers, int numberOfDice, DieType dieType, ComparisonOperator comparisonOperator, int value, int expected)
        {
            var evaluator = new DiceEvaluator(new TestHelpers.MockRandomNumberGenerator(numbers));
            var modifiers = new List<Modifier> { new FailureModifier(comparisonOperator, value) };
            var basicRoll = new BasicRoll(numberOfDice, dieType, modifiers);
            Assert.AreEqual(expected, evaluator.Evaluate(basicRoll));
        }

        public static IEnumerable<TestCaseData> Evaluate_BasicRoll_WithFailureModifier_TestCases()
        {
            // Test case 1: Count failures for 3d6 where failure is < 3
            yield return new TestCaseData(new List<int> { 5, 2, 6 }, 3, new DieType.Basic(6), ComparisonOperator.LessThan, 3, -1);
            // Test case 2: Count failures for 5d10 where failure is < 4
            yield return new TestCaseData(new List<int> { 3, 7, 2, 9, 1 }, 5, new DieType.Basic(10), ComparisonOperator.LessThan, 4, -3);
            // Test case 3: Count failures for 4d20 where failure is = 1 (critical failure)
            yield return new TestCaseData(new List<int> { 1, 15, 1, 8 }, 4, new DieType.Basic(20), ComparisonOperator.Equal, 1, -2);
            // Test case 4: Count failures for 6d4 where failure is < 2
            yield return new TestCaseData(new List<int> { 1, 3, 1, 4, 2, 1 }, 6, new DieType.Basic(4), ComparisonOperator.LessThan, 2, -3);

            // Test case 5: Count failures for 2d100 where failure is > 90 (percentile check)
            yield return new TestCaseData(new List<int> { 95, 80 }, 2, new DieType.Percent(), ComparisonOperator.GreaterThan, 90, -1);

            // Test case 6: Count failures for 5d6 where failure is = 1
            yield return new TestCaseData(new List<int> { 1, 3, 1, 5, 6 }, 5, new DieType.Basic(6), ComparisonOperator.Equal, 1, -2);

            // Test case 7: Count failures for 7d8 where failure is < 3
            yield return new TestCaseData(new List<int> { 2, 1, 5, 7, 8, 3, 2 }, 7, new DieType.Basic(8), ComparisonOperator.LessThan, 3, -3);

            // Test case 8: Count failures for 4fudge (fudge dice) where failure is < 0
            yield return new TestCaseData(new List<int> { -1, 0, -1, 1 }, 4, new DieType.Fudge(), ComparisonOperator.LessThan, 0, -2);

            // Test case 9: Count failures for 3d12 where failure is > 8
            yield return new TestCaseData(new List<int> { 9, 7, 12 }, 3, new DieType.Basic(12), ComparisonOperator.GreaterThan, 8, -2);

            // Test case 10: Count failures for 10d6 where failure is < 3
            yield return new TestCaseData(new List<int> { 2, 1, 4, 5, 2, 6, 3, 1, 5, 2 }, 10, new DieType.Basic(6), ComparisonOperator.LessThan, 3, -5);
        }

        [Theory]
        [TestCaseSource(nameof(Evaluate_BasicRoll_WithConstantModifier_TestCases))]
        public void Evaluate_BasicRoll_WithConstantModifier(List<int> numbers, List<Modifier> mods, int expected)
        {
            var evaluator = new DiceEvaluator(new TestHelpers.MockRandomNumberGenerator(numbers));
            var basicRoll = new BasicRoll(numbers.Count, new DieType.Basic(6), mods);
            Assert.AreEqual(expected, evaluator.Evaluate(basicRoll));
        }

        public static IEnumerable<TestCaseData> Evaluate_BasicRoll_WithConstantModifier_TestCases()
        {
            yield return new TestCaseData(new List<int> { 5, 2 }, new List<Modifier> { new ConstantModifier(ArithmeticOperator.Add, 3) }, 10);
            yield return new TestCaseData(new List<int> { 5, 2 }, new List<Modifier> { new ConstantModifier(ArithmeticOperator.Subtract, 3) }, 4);
        }

        [Theory]
        [TestCaseSource(nameof(Evaluate_BasicRoll_WithConstantModifierAndAnotherModifier_TestCases))]
        public void Evaluate_BasicRoll_WithConstantModifierAndAnotherModifier(List<int> numbers, List<Modifier> mods, int expected)
        {
            var evaluator = new DiceEvaluator(new TestHelpers.MockRandomNumberGenerator(numbers));
            var modifiers = mods;
            var basicRoll = new BasicRoll(numbers.Count, new DieType.Basic(6), modifiers);
            Assert.AreEqual(expected, evaluator.Evaluate(basicRoll));
        }

        public static IEnumerable<TestCaseData> Evaluate_BasicRoll_WithConstantModifierAndAnotherModifier_TestCases()
        {
            yield return new TestCaseData(new List<int> { 5, 2 }, new List<Modifier> { new KeepModifier(1, true), new ConstantModifier(ArithmeticOperator.Add, 6) }, 11);
            yield return new TestCaseData(new List<int> { 5, 2 }, new List<Modifier> { new ConstantModifier(ArithmeticOperator.Add, 6), new KeepModifier(1, true) }, 11);
        }

        [Test]
        public void Evaluate_UnknownRollType_ThrowsArgumentException()
        {
            var evaluator = new DiceEvaluator();
            var unknownRoll = new UnknownRoll();
            Assert.Throws<ArgumentException>(() => evaluator.Evaluate(unknownRoll));
        }

        [Theory]
        [TestCaseSource(nameof(Evaluate_ErrorCases_TestCases))]
        public void Evaluate_ErrorCases_HandlesCorrectly(Action testAction, Type expectedExceptionType)
        {
            Assert.Throws(expectedExceptionType, () => testAction());
        }

        public static IEnumerable<TestCaseData> Evaluate_ErrorCases_TestCases()
        {
            // Test case 1: Setting MaxExplosions to 0 should throw ArgumentOutOfRangeException
            yield return new TestCaseData(
                new Action(() => { 
                    var evaluator = new DiceEvaluator();
                    evaluator.MaxExplosions = 0;
                }), 
                typeof(ArgumentOutOfRangeException));
                
            // Test case 2: Setting MaxCompounds to negative number should throw ArgumentOutOfRangeException
            yield return new TestCaseData(
                new Action(() => { 
                    var evaluator = new DiceEvaluator();
                    evaluator.MaxCompounds = -5;
                }), 
                typeof(ArgumentOutOfRangeException));
                
            // Test case 3: Evaluating unknown roll type should throw ArgumentException
            yield return new TestCaseData(
                new Action(() => { 
                    var evaluator = new DiceEvaluator();
                    var unknownRoll = new UnknownRoll();
                    evaluator.Evaluate(unknownRoll);
                }), 
                typeof(ArgumentException));
                
            // Test case 4: Attempting to roll a die with unknown die type should throw ArgumentException
            yield return new TestCaseData(
                new Action(() => { 
                    var evaluator = new DiceEvaluator();
                    // Creating a non-standard die type by reflection or mock would go here
                    // For this test, we'll leverage UnknownRoll with the right casting
                    var dieType = new DieType.Constant(); // This is not handled in RollDie
                    var basicRoll = new BasicRoll(1, dieType, new List<Modifier>());
                    evaluator.Evaluate(basicRoll);
                }), 
                typeof(ArgumentException));
                
            // Test case 5: Invalid comparison operator should throw InvalidEnumArgumentException
            yield return new TestCaseData(
                new Action(() => { 
                    var evaluator = new DiceEvaluator();
                    // We'd need to create an invalid ComparisonOperator value 
                    // This would require unsafe code, so we'll simulate it by testing 
                    // a modifier that internally calls Compare with an invalid operator
                    var compOp = (ComparisonOperator)999; // Invalid value
                    var mock = new TestHelpers.MockRandomNumberGenerator(new List<int> { 1 });
                    var failModifier = new FailureModifier(compOp, 1);
                    var basicRoll = new BasicRoll(1, new DieType.Basic(6), new List<Modifier> { failModifier });
                    evaluator.Evaluate(basicRoll);
                }), 
                typeof(InvalidEnumArgumentException));
        }

        [Theory]
        [TestCaseSource(nameof(Evaluate_RealWorldScenarios_TestCases))]
        public void Evaluate_RealWorldScenarios_CalculatesCorrectly(IRandomNumberGenerator<int> rng, Roll roll, int expected, string scenarioDescription)
        {
            var evaluator = new DiceEvaluator(rng);
            Assert.AreEqual(expected, evaluator.Evaluate(roll), $"Failed scenario: {scenarioDescription}");
        }

        public static IEnumerable<TestCaseData> Evaluate_RealWorldScenarios_TestCases()
        {
            // D&D Ability Score Generation: 4d6, drop lowest, add modifier
            yield return new TestCaseData(
                new TestHelpers.MockRandomNumberGenerator(new List<int> { 6, 4, 3, 1 }),
                new BasicRoll(4, new DieType.Basic(6), new List<Modifier> { 
                    new DropModifier(1, false) 
                }),
                13,
                "D&D Ability Score Generation: 4d6 drop lowest (6,4,3,1) = 13"
            );

            // D&D Advantage Roll: 2d20 keep highest
            yield return new TestCaseData(
                new TestHelpers.MockRandomNumberGenerator(new List<int> { 15, 20 }),
                new BasicRoll(2, new DieType.Basic(20), new List<Modifier> { 
                    new KeepModifier(1, true) 
                }),
                20,
                "D&D Advantage Roll: 2d20 keep highest (15,20) = 20"
            );

            // D&D Disadvantage Roll: 2d20 keep lowest
            yield return new TestCaseData(
                new TestHelpers.MockRandomNumberGenerator(new List<int> { 12, 5 }),
                new BasicRoll(2, new DieType.Basic(20), new List<Modifier> { 
                    new KeepModifier(1, false) 
                }),
                5,
                "D&D Disadvantage Roll: 2d20 keep lowest (12,5) = 5"
            );

            // D&D Attack with Modifier: 1d20 + 5 (Proficiency + Ability)
            yield return new TestCaseData(
                new TestHelpers.MockRandomNumberGenerator(new List<int> { 18 }),
                new BasicRoll(1, new DieType.Basic(20), new List<Modifier> { 
                    new ConstantModifier(ArithmeticOperator.Add, 5) 
                }),
                23,
                "D&D Attack with Modifier: 1d20+5 (18+5) = 23"
            );

            // D&D Critical Hit: 2d6 + 2d6 (crit) + 3 (modifier)
            yield return new TestCaseData(
                new TestHelpers.MockRandomNumberGenerator(new List<int> { 4, 6, 5, 3, 20 }),
                new BasicRoll(4, new DieType.Basic(6), new List<Modifier> { 
                    new ConstantModifier(ArithmeticOperator.Add, 3) 
                }),
                21,
                "D&D Critical Hit: 4d6+3 (4,6,5,3+3) = 21"
            );

            // Shadowrun Success Test: 5d6, count 5s and 6s as successes
            yield return new TestCaseData(
                new TestHelpers.MockRandomNumberGenerator(new List<int> { 6, 2, 5, 3, 1 }),
                new BasicRoll(5, new DieType.Basic(6), new List<Modifier> { 
                    new SuccessModifier(ComparisonOperator.GreaterThan, 4) 
                }),
                2,
                "Shadowrun Success Test: 5d6, count 5+ (6,2,5,3,1) = 2 successes"
            );

            // World of Darkness: 4d10, 8+ success, 10s explode
            yield return new TestCaseData(
                new TestHelpers.MockRandomNumberGenerator(new List<int> { 10, 7, 9, 3, 8 }),
                new BasicRoll(4, new DieType.Basic(10), new List<Modifier> { 
                    new ExplodeModifier(ComparisonOperator.Equal, 10),
                    new SuccessModifier(ComparisonOperator.GreaterThan, 7)
                }),
                3,
                "World of Darkness: 4d10, 8+ success, 10s explode (10->8,7,9,3) = 3 successes"
            );

            // FATE/Fudge: 4dF + 2 (skill)
            yield return new TestCaseData(
                new TestHelpers.MockRandomNumberGenerator(new List<int> { 1, -1, 0, 1 }),
                new BasicRoll(4, new DieType.Fudge(), new List<Modifier> { 
                    new ConstantModifier(ArithmeticOperator.Add, 2) 
                }),
                3,
                "FATE/Fudge: 4dF+2 (1,-1,0,1+2) = 3"
            );

            // Complex Attack Roll: 2d20 advantage, keep highest, add 5, crit on 20
            yield return new TestCaseData(
                new TestHelpers.MockRandomNumberGenerator(new List<int> { 20, 12, 6 }),
                new BasicRoll(2, new DieType.Basic(20), new List<Modifier> { 
                    new KeepModifier(1, true),
                    new ExplodeModifier(ComparisonOperator.Equal, 20),
                    new ConstantModifier(ArithmeticOperator.Add, 5)
                }),
                31, // 20 (first roll) + 6 (explosion) + 5 (modifier) = 31
                "Complex Attack: 1d20 advantage, explode on 20, +5 modifier (20->6, 5) = 31"
            );

            // Call of Cthulhu: d100 check against skill of 50, lower is better
            yield return new TestCaseData(
                new TestHelpers.MockRandomNumberGenerator(new List<int> { 23 }),
                new BasicRoll(1, new DieType.Percent(), new List<Modifier> { 
                    new SuccessModifier(ComparisonOperator.LessThan, 50) 
                }),
                1, // Hard success
                "Call of Cthulhu: d100 vs skill 50 (23) = 1 success"
            );

            // Mixed Dice Pool: 2d6 + 1d8 + 1d4, drop lowest
            yield return new TestCaseData(
                new TestHelpers.MockRandomNumberGenerator(new List<int> { 3, 5, 7, 2 }),
                new BasicRoll(4, new DieType.Basic(8), new List<Modifier> { 
                    new DropModifier(1, false)
                }),
                15, // 3 + 5 + 7 = 15 (drop 2)
                "Mixed Dice Pool: 2d6+1d8+1d4, drop lowest (3,5,7,2) = 15"
            );
            
            // Dice Chain with Multiple Modifiers: 3d6, explode on 6s, keep 2 highest, add 4
            yield return new TestCaseData(
                new TestHelpers.MockRandomNumberGenerator(new List<int> { 6, 3, 4, 6, 5 }),
                new BasicRoll(3, new DieType.Basic(6), new List<Modifier> { 
                    new ExplodeModifier(ComparisonOperator.Equal, 6),
                    new KeepModifier(2, true),
                    new ConstantModifier(ArithmeticOperator.Add, 4)
                }),
                16, // (6 + 6) + 4 = 16
                "Dice Chain: 3d6, explode 6s, keep 2 highest, +4 (6->6,3,4, c4) = 16"
            );

            // Shadowrun with compound modifier: 3d6, compound on 6s, keep 2 highest, add 4
            yield return new TestCaseData(
                new TestHelpers.MockRandomNumberGenerator(new List<int> { 6, 3, 4, 6, 5 }),
                new BasicRoll(3, new DieType.Basic(6), new List<Modifier> { 
                    new CompoundingModifier(ComparisonOperator.Equal, 6),
                    new KeepModifier(2, true),
                    new ConstantModifier(ArithmeticOperator.Add, 4)
                }),
                25, // (6 + 6 + 5) + 4 + 4 = 25
                "Shadowrun with compound modifier: 3d6, compound on 6s, keep 2 highest, +4 (6->6->5,3,4, c4) = 25"
            );
        }

        [Theory]
        [TestCaseSource(nameof(Evaluate_MultipleRoll_Scenarios_TestCases))]
        public void Evaluate_MultipleRoll_CombinesProperly(List<int> randomNumbers, List<Roll> rolls, 
            Func<List<int>, int> resultCombiner, int expected, string description)
        {
            // Setup evaluator with controlled random number generation
            var mockRng = new MultiRollRandomNumberGenerator(randomNumbers);
            var evaluator = new DiceEvaluator(mockRng);
            
            // Evaluate each roll individually
            var results = rolls.Select(r => evaluator.Evaluate(r)).ToList();
            
            // Apply the custom combining function to the results
            int combinedResult = resultCombiner(results);
            
            // Assert the combined result matches the expected value
            Assert.AreEqual(expected, combinedResult, description);
        }
        
        public static IEnumerable<TestCaseData> Evaluate_MultipleRoll_Scenarios_TestCases()
        {
            // Savage Worlds: Roll trait die (d8) and wild die (d6), take the higher result
            // First roll is d8 with explosion (8->6), second roll is d6 with explosion (6->4)
            yield return new TestCaseData(
                new List<int> { 8, 6, 6, 4 },                    // Random numbers for all rolls
                new List<Roll> {                                // List of rolls to make
                    new BasicRoll(1, new DieType.Basic(8), new List<Modifier> { 
                        new ExplodeModifier(ComparisonOperator.Equal, 8) 
                    }),
                    new BasicRoll(1, new DieType.Basic(6), new List<Modifier> { 
                        new ExplodeModifier(ComparisonOperator.Equal, 6) 
                    })
                },
                new Func<List<int>, int>(results => results.Max()),  // Take the higher result
                14,                                                 // Expected result (8+6 vs 6+4)
                "Savage Worlds: d8! = 8->6 (14), d6! = 6->4 (10), result = 14"
            );
            
            // Savage Worlds: Roll trait die (d6) and wild die (d6), take the higher result
            // First roll is d6 = 2, second roll is d6 with explosion (6->6->2)
            yield return new TestCaseData(
                new List<int> { 2, 6, 6, 2 },
                new List<Roll> {
                    new BasicRoll(1, new DieType.Basic(6), new List<Modifier>()),
                    new BasicRoll(1, new DieType.Basic(6), new List<Modifier> { 
                        new ExplodeModifier(ComparisonOperator.Equal, 6) 
                    })
                },
                new Func<List<int>, int>(results => results.Max()),
                14,                                               // Expected result (14 > 2)
                "Savage Worlds: d6 = 2, d6! = 6->6->2 (14), result = 14"
            );
            
            // D&D Attack + Damage: Roll d20 for attack, if >= 15 then roll 2d6+3 damage, otherwise 0
            yield return new TestCaseData(
                new List<int> { 18, 4, 6 },
                new List<Roll> {
                    new BasicRoll(1, new DieType.Basic(20), new List<Modifier>()),
                    new BasicRoll(2, new DieType.Basic(6), new List<Modifier> { 
                        new ConstantModifier(ArithmeticOperator.Add, 3) 
                    })
                },
                new Func<List<int>, int>(results => results[0] >= 15 ? results[1] : 0),
                13,                                              // Expected damage (4+6+3 = 13)
                "D&D Attack + Damage: d20 = 18 (hit), 2d6+3 = 13 damage"
            );
            
            // D&D Attack + Damage: Roll d20 for attack, if >= 15 then roll 2d6+3 damage, otherwise 0
            yield return new TestCaseData(
                new List<int> { 12, 4, 6 },
                new List<Roll> {
                    new BasicRoll(1, new DieType.Basic(20), new List<Modifier>()),
                    new BasicRoll(2, new DieType.Basic(6), new List<Modifier> { 
                        new ConstantModifier(ArithmeticOperator.Add, 3) 
                    })
                },
                new Func<List<int>, int>(results => results[0] >= 15 ? results[1] : 0),
                0,                                               // No damage (attack missed)
                "D&D Attack + Damage: d20 = 12 (miss), no damage dealt"
            );
            
            // Call of Cthulhu: Roll d100 skill check (lower is better) and d100 luck check
            // If skill succeeds (result <= 50), return success value 1
            // If skill fails but luck succeeds (luck <= 30), return partial success value 0.5
            // Otherwise, return failure value 0
            yield return new TestCaseData(
                new List<int> { 35, 25 },
                new List<Roll> {
                    new BasicRoll(1, new DieType.Percent(), new List<Modifier>()),
                    new BasicRoll(1, new DieType.Percent(), new List<Modifier>())
                },
                new Func<List<int>, int>(results => {
                    if (results[0] <= 50) return 1;       // Skill check success
                    if (results[1] <= 30) return 0;       // Luck saved (partial success)
                    return -1;                            // Complete failure
                }),
                1,                                        // Expected result (skill check succeeded)
                "Call of Cthulhu: Skill d100 = 35 (success), Luck d100 = 25 (not needed)"
            );
            
            // Combined Damage Roll: Roll 1d8 (slashing) + 1d6 (fire) + 1d4 (poison)
            yield return new TestCaseData(
                new List<int> { 5, 4, 2 },
                new List<Roll> {
                    new BasicRoll(1, new DieType.Basic(8), new List<Modifier>()),
                    new BasicRoll(1, new DieType.Basic(6), new List<Modifier>()),
                    new BasicRoll(1, new DieType.Basic(4), new List<Modifier>())
                },
                new Func<List<int>, int>(results => results.Sum()),
                11,                                       // Expected result (5+4+2 = 11)
                "Combined Damage: 1d8 (5) + 1d6 (4) + 1d4 (2) = 11"
            );
            
            // Resistance Check: Roll 2d10 keep highest (resistance) vs 2d10 keep highest (difficulty)
            // Succeed if resistance >= difficulty
            yield return new TestCaseData(
                new List<int> { 8, 5, 4, 7 },
                new List<Roll> {
                    new BasicRoll(2, new DieType.Basic(10), new List<Modifier> { new KeepModifier(1, true) }),
                    new BasicRoll(2, new DieType.Basic(10), new List<Modifier> { new KeepModifier(1, true) })
                },
                new Func<List<int>, int>(results => results[0] >= results[1] ? 1 : 0),
                1,                                        // Expected result (8 >= 7)
                "Resistance Check: Resistance 2d10kh1 = 8, Difficulty 2d10kh1 = 7, success = 1"
            );
            
            // Critical Hit Check: Roll 1d20 for attack, if = 20, roll another d20 to confirm critical
            // Critical confirmed if second roll >= 15
            yield return new TestCaseData(
                new List<int> { 20, 17 },
                new List<Roll> {
                    new BasicRoll(1, new DieType.Basic(20), new List<Modifier>()),
                    new BasicRoll(1, new DieType.Basic(20), new List<Modifier>())
                },
                new Func<List<int>, int>(results => results[0] == 20 && results[1] >= 15 ? 2 : 
                                                   results[0] == 20 ? 1 : 0),
                2,                                        // Expected result (critical confirmed)
                "Critical Hit Check: Attack d20 = 20, Confirmation d20 = 17 (confirmed critical)"
            );
            
            // Shadowrun Extended Test: Roll 5d6 for initial test (count 5+ as successes), 
            // then roll 3d6 for second attempt (count 5+ as successes)
            // Need a total of 3 successes across both rolls
            yield return new TestCaseData(
                new List<int> { 6, 3, 2, 5, 1, 5, 6, 4 },
                new List<Roll> {
                    new BasicRoll(5, new DieType.Basic(6), new List<Modifier> { 
                        new SuccessModifier(ComparisonOperator.GreaterThan, 4) 
                    }),
                    new BasicRoll(3, new DieType.Basic(6), new List<Modifier> { 
                        new SuccessModifier(ComparisonOperator.GreaterThan, 4) 
                    })
                },
                new Func<List<int>, int>(results => results.Sum() >= 3 ? 1 : 0),
                1,                                        // Expected result (2 + 2 = 4 successes)
                "Shadowrun Extended Test: First roll = 2 successes, Second roll = 2 successes, Total = 4 (success)"
            );
            
            // Opposed Check: Roll 4d6 drop lowest vs 3d6 drop lowest, higher result wins
            yield return new TestCaseData(
                new List<int> { 5, 3, 1, 6, 4, 4, 2 },
                new List<Roll> {
                    new BasicRoll(4, new DieType.Basic(6), new List<Modifier> { 
                        new DropModifier(1, false) 
                    }),
                    new BasicRoll(3, new DieType.Basic(6), new List<Modifier> { 
                        new DropModifier(1, false) 
                    })
                },
                new Func<List<int>, int>(results => results[0] > results[1] ? 1 : 
                                                  results[0] < results[1] ? -1 : 0),
                1,                                        // Expected result (14 > 8, first roll wins)
                "Opposed Check: First roll 4d6dl1 = 14, Second roll 3d6dl1 = 8, First roll wins"
            );
        }

        #region ArithmeticRoll Tests

        [Test]
        public void Evaluate_ArithmeticRoll_SimpleAddition_ReturnsCorrectResult()
        {
            // Arrange
            var mockRng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 3, 5 });
            var evaluator = new DiceEvaluator(mockRng);
            
            var roll = new ArithmeticRoll(new List<(ArithmeticOperator, Roll)>
            {
                (ArithmeticOperator.Add, new BasicRoll(1, new DieType.Basic(6), new List<Modifier>())),
                (ArithmeticOperator.Add, new BasicRoll(1, new DieType.Basic(8), new List<Modifier>()))
            });

            // Act
            int result = evaluator.Evaluate(roll);

            // Assert
            Assert.That(result, Is.EqualTo(8), "1d6(3) + 1d8(5) should equal 8");
        }

        [Test]
        public void Evaluate_ArithmeticRoll_SimpleSubtraction_ReturnsCorrectResult()
        {
            // Arrange
            var mockRng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 15, 3 });
            var evaluator = new DiceEvaluator(mockRng);
            
            var roll = new ArithmeticRoll(new List<(ArithmeticOperator, Roll)>
            {
                (ArithmeticOperator.Add, new BasicRoll(1, new DieType.Basic(20), new List<Modifier>())),
                (ArithmeticOperator.Subtract, new BasicRoll(1, new DieType.Basic(4), new List<Modifier>()))
            });

            // Act
            int result = evaluator.Evaluate(roll);

            // Assert
            Assert.That(result, Is.EqualTo(12), "1d20(15) - 1d4(3) should equal 12");
        }

        [Test]
        public void Evaluate_ArithmeticRoll_WithConstants_ReturnsCorrectResult()
        {
            // Arrange
            var mockRng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 10 });
            var evaluator = new DiceEvaluator(mockRng);
            
            var roll = new ArithmeticRoll(new List<(ArithmeticOperator, Roll)>
            {
                (ArithmeticOperator.Add, new BasicRoll(1, new DieType.Basic(12), new List<Modifier>())),
                (ArithmeticOperator.Add, new Constant(5)),
                (ArithmeticOperator.Subtract, new Constant(2))
            });

            // Act
            int result = evaluator.Evaluate(roll);

            // Assert
            Assert.That(result, Is.EqualTo(13), "1d12(10) + 5 - 2 should equal 13");
        }

        [Test]
        public void Evaluate_ArithmeticRoll_MultipleTerms_EvaluatesLeftToRight()
        {
            // Arrange
            var mockRng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 6, 4, 2 });
            var evaluator = new DiceEvaluator(mockRng);
            
            var roll = new ArithmeticRoll(new List<(ArithmeticOperator, Roll)>
            {
                (ArithmeticOperator.Add, new BasicRoll(1, new DieType.Basic(6), new List<Modifier>())),
                (ArithmeticOperator.Add, new BasicRoll(1, new DieType.Basic(6), new List<Modifier>())),
                (ArithmeticOperator.Subtract, new BasicRoll(1, new DieType.Basic(4), new List<Modifier>())),
                (ArithmeticOperator.Add, new Constant(3))
            });

            // Act
            int result = evaluator.Evaluate(roll);

            // Assert
            Assert.That(result, Is.EqualTo(11), "1d6(6) + 1d6(4) - 1d4(2) + 3 should equal 11 (evaluated left to right)");
        }

        [Test]
        public void Evaluate_ArithmeticRoll_WithModifiers_ReturnsCorrectResult()
        {
            // Arrange - d6 with explode, d4 with reroll once
            var mockRng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 6, 3, 1, 4 });
            var evaluator = new DiceEvaluator(mockRng);
            
            var roll = new ArithmeticRoll(new List<(ArithmeticOperator, Roll)>
            {
                (ArithmeticOperator.Add, new BasicRoll(1, new DieType.Basic(6), new List<Modifier>
                {
                    new ExplodeModifier(ComparisonOperator.Equal, 6)
                })),
                (ArithmeticOperator.Subtract, new BasicRoll(1, new DieType.Basic(4), new List<Modifier>
                {
                    new RerollOnceModifier(ComparisonOperator.LessThan, 2)
                }))
            });

            // Act
            int result = evaluator.Evaluate(roll);

            // Assert
            Assert.That(result, Is.EqualTo(5), "1d6!(6+3=9) - 1d4ro<2(1->4) should equal 5");
        }

        [Test]
        public void Evaluate_ArithmeticRoll_NegativeResult_ReturnsNegativeValue()
        {
            // Arrange
            var mockRng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 2, 8 });
            var evaluator = new DiceEvaluator(mockRng);
            
            var roll = new ArithmeticRoll(new List<(ArithmeticOperator, Roll)>
            {
                (ArithmeticOperator.Add, new BasicRoll(1, new DieType.Basic(4), new List<Modifier>())),
                (ArithmeticOperator.Subtract, new BasicRoll(1, new DieType.Basic(8), new List<Modifier>()))
            });

            // Act
            int result = evaluator.Evaluate(roll);

            // Assert
            Assert.That(result, Is.EqualTo(-6), "1d4(2) - 1d8(8) should equal -6");
        }

        [Test]
        public void Evaluate_ArithmeticRoll_ZeroResult_ReturnsZero()
        {
            // Arrange
            var mockRng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 5 });
            var evaluator = new DiceEvaluator(mockRng);
            
            var roll = new ArithmeticRoll(new List<(ArithmeticOperator, Roll)>
            {
                (ArithmeticOperator.Add, new BasicRoll(1, new DieType.Basic(6), new List<Modifier>())),
                (ArithmeticOperator.Subtract, new Constant(5))
            });

            // Act
            int result = evaluator.Evaluate(roll);

            // Assert
            Assert.That(result, Is.EqualTo(0), "1d6(5) - 5 should equal 0");
        }

        [Test]
        public void Evaluate_ArithmeticRoll_OnlyConstants_ReturnsCorrectResult()
        {
            // Arrange
            var evaluator = new DiceEvaluator(new RandomIntGenerator());
            
            var roll = new ArithmeticRoll(new List<(ArithmeticOperator, Roll)>
            {
                (ArithmeticOperator.Add, new Constant(10)),
                (ArithmeticOperator.Add, new Constant(7)),
                (ArithmeticOperator.Subtract, new Constant(3))
            });

            // Act
            int result = evaluator.Evaluate(roll);

            // Assert
            Assert.That(result, Is.EqualTo(14), "10 + 7 - 3 should equal 14");
        }

        [Test]
        public void Evaluate_ArithmeticRoll_ComplexExpression_ReturnsCorrectResult()
        {
            // Arrange - Simulate the example from the issue: 3d20+5d6-1d4ro<2+1
            var mockRng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 10, 15, 8, 4, 5, 6, 2, 3, 1, 3 });
            var evaluator = new DiceEvaluator(mockRng);
            
            var roll = new ArithmeticRoll(new List<(ArithmeticOperator, Roll)>
            {
                (ArithmeticOperator.Add, new BasicRoll(3, new DieType.Basic(20), new List<Modifier>())),
                (ArithmeticOperator.Add, new BasicRoll(5, new DieType.Basic(6), new List<Modifier>())),
                (ArithmeticOperator.Subtract, new BasicRoll(1, new DieType.Basic(4), new List<Modifier>
                {
                    new RerollOnceModifier(ComparisonOperator.LessThan, 2)
                })),
                (ArithmeticOperator.Add, new Constant(1))
            });

            // Act
            int result = evaluator.Evaluate(roll);

            // Assert
            // 3d20: 10+15+8=33, 5d6: 4+5+6+2+3=20, 1d4ro<2: 1 rerolled to 3, total: 33+20-3+1=51
            Assert.That(result, Is.EqualTo(51), "Complex expression should evaluate correctly");
        }

        [Test]
        public void Evaluate_ArithmeticRoll_LargeNumbers_HandlesCorrectly()
        {
            // Arrange
            var mockRng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 100 });
            var evaluator = new DiceEvaluator(mockRng);
            
            var roll = new ArithmeticRoll(new List<(ArithmeticOperator, Roll)>
            {
                (ArithmeticOperator.Add, new BasicRoll(1, new DieType.Basic(100), new List<Modifier>())),
                (ArithmeticOperator.Add, new Constant(1000)),
                (ArithmeticOperator.Subtract, new Constant(50))
            });

            // Act
            int result = evaluator.Evaluate(roll);

            // Assert
            Assert.That(result, Is.EqualTo(1050), "1d100(100) + 1000 - 50 should equal 1050");
        }

        [Test]
        public void Evaluate_ArithmeticRoll_SuccessModifiers_CalculatesCorrectly()
        {
            // Arrange - Roll 5d6 with success on 5+, then add a constant
            var mockRng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 6, 3, 5, 2, 4 });
            var evaluator = new DiceEvaluator(mockRng);
            
            var roll = new ArithmeticRoll(new List<(ArithmeticOperator, Roll)>
            {
                (ArithmeticOperator.Add, new BasicRoll(5, new DieType.Basic(6), new List<Modifier>
                {
                    new SuccessModifier(ComparisonOperator.GreaterThan, 4)
                })),
                (ArithmeticOperator.Add, new Constant(10))
            });

            // Act
            int result = evaluator.Evaluate(roll);

            // Assert
            // 5d6 with success on 5+: 6(success), 3(fail), 5(success), 2(fail), 4(fail) = 2 successes
            Assert.That(result, Is.EqualTo(12), "5d6 success count (2) + 10 should equal 12");
        }

        [Test]
        public void Evaluate_ArithmeticRoll_WithInvalidOperator_ThrowsException()
        {
            // Arrange
            var evaluator = new DiceEvaluator(new RandomIntGenerator());
            
            // Create a roll with an invalid operator (casting an invalid enum value)
            var roll = new ArithmeticRoll(new List<(ArithmeticOperator, Roll)>
            {
                (ArithmeticOperator.Add, new Constant(5)),
                ((ArithmeticOperator)999, new Constant(3)) // Invalid operator
            });

            // Act & Assert
            Assert.Throws<ArgumentException>(() => evaluator.Evaluate(roll));
        }

        #endregion
    }
}