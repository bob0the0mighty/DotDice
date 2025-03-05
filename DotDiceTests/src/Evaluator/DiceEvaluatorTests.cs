using DotDice.Evaluator;
using DotDice.Parser;
using DotDice.RandomNumberGenerator;

namespace DotDice.Tests
{
    [TestFixture]
    public class DiceEvaluatorTests
    {
        [Test]
        public void Evaluate_ConstantRoll_ReturnsConstantValue()
        {
            var evaluator = new DiceEvaluator();
            var constant = new Constant(5);
            Assert.AreEqual(5, evaluator.Evaluate(constant));
        }

        [Theory]
        [TestCaseSource(nameof(Evaluate_BasicRoll_ReturnsSumOfDice_TestCases))]
        public void Evaluate_BasicRoll_ReturnsSumOfDice(List<int> numbers, int expected)
        {
            var evaluator = new DiceEvaluator(new MockRandomNumberGenerator(numbers));
            var basicRoll = new BasicRoll(numbers.Count, new DieType.Basic(6), new List<Modifier>());
            Assert.AreEqual(expected, evaluator.Evaluate(basicRoll));
        }

        public static IEnumerable<TestCaseData> Evaluate_BasicRoll_ReturnsSumOfDice_TestCases()
        {
            yield return new TestCaseData(new List<int> { 5, 2 }, 7);
            yield return new TestCaseData(new List<int> { 10, 5, 2 }, 17);
        }

        [Test]
        public void Evaluate_PercentRoll_ReturnsSumOfDice()
        {
            var evaluator = new DiceEvaluator(new MockRandomNumberGenerator(new List<int> { 50, 75 }));
            var basicRoll = new BasicRoll(2, new DieType.Percent(), new List<Modifier>());
            Assert.AreEqual(125, evaluator.Evaluate(basicRoll));
        }

        [Test]
        public void Evaluate_FudgeRoll_ReturnsSumOfDice()
        {
            var evaluator = new DiceEvaluator(new MockRandomNumberGenerator(new List<int> { -1, 0, 1 }));
            var basicRoll = new BasicRoll(3, new DieType.Fudge(), new List<Modifier>());
            Assert.AreEqual(0, evaluator.Evaluate(basicRoll));

            evaluator = new DiceEvaluator(new MockRandomNumberGenerator(new List<int> { 1, 0, 1 }));
            basicRoll = new BasicRoll(3, new DieType.Fudge(), new List<Modifier>());
            Assert.AreEqual(2, evaluator.Evaluate(basicRoll));
        }

        [Test]
        public void Evaluate_BasicRoll_WithKeepHighestModifier()
        {
            var evaluator = new DiceEvaluator(new MockRandomNumberGenerator(new List<int> { 5, 2 }));
            var modifiers = new List<Modifier> { new KeepModifier(1, true) };
            var basicRoll = new BasicRoll(2, new DieType.Basic(6), modifiers);
            Assert.AreEqual(5, evaluator.Evaluate(basicRoll));
        }

        [Test]
        public void Evaluate_BasicRoll_WithKeepLowestModifier()
        {
            var evaluator = new DiceEvaluator(new MockRandomNumberGenerator(new List<int> { 5, 2 }));
            var modifiers = new List<Modifier> { new KeepModifier(1, false) };
            var basicRoll = new BasicRoll(2, new DieType.Basic(6), modifiers);
            Assert.AreEqual(2, evaluator.Evaluate(basicRoll));
        }

        [Test]
        public void Evaluate_BasicRoll_WithDropHighestModifier()
        {
            var evaluator = new DiceEvaluator(new MockRandomNumberGenerator(new List<int> { 5, 2 }));
            var modifiers = new List<Modifier> { new DropModifier(1, true) };
            var basicRoll = new BasicRoll(2, new DieType.Basic(6), modifiers);
            Assert.AreEqual(2, evaluator.Evaluate(basicRoll));
        }

        [Test]
        public void Evaluate_BasicRoll_WithDropLowestModifier()
        {
            var evaluator = new DiceEvaluator(new MockRandomNumberGenerator(new List<int> { 5, 2 }));
            var modifiers = new List<Modifier> { new DropModifier(1, false) };
            var basicRoll = new BasicRoll(2, new DieType.Basic(6), modifiers);
            Assert.AreEqual(5, evaluator.Evaluate(basicRoll));
        }

        [Test]
        public void Evaluate_BasicRoll_WithRerollOnceModifier()
        {
            var evaluator = new DiceEvaluator(new MockRandomNumberGenerator(new List<int> { 1, 5 }));
            var modifiers = new List<Modifier> { new RerollOnceModifier(ComparisonOperator.LessThan, 2) };
            var basicRoll = new BasicRoll(1, new DieType.Basic(6), modifiers);
            Assert.AreEqual(5, evaluator.Evaluate(basicRoll));
        }

        [Test]
        public void Evaluate_BasicRoll_WithRerollUntilModifier()
        {
            var evaluator = new DiceEvaluator(new MockRandomNumberGenerator(new List<int> { 1, 5, 6 }));
            var modifiers = new List<Modifier> { new RerollUntilModifier(ComparisonOperator.LessThan, 2) };
            var basicRoll = new BasicRoll(1, new DieType.Basic(6), modifiers);
            Assert.AreEqual(5, evaluator.Evaluate(basicRoll));
        }

        [Test]
        public void Evaluate_BasicRoll_WithExplodeModifier()
        {
            var evaluator = new DiceEvaluator(new MockRandomNumberGenerator(new List<int> { 6, 2 }));
            var modifiers = new List<Modifier> { new ExplodeModifier(ComparisonOperator.GreaterThan, 5) };
            var basicRoll = new BasicRoll(1, new DieType.Basic(6), modifiers);
            Assert.AreEqual(8, evaluator.Evaluate(basicRoll));
        }

        [Test]
        public void Evaluate_BasicRoll_WithCompoundingModifier()
        {
            var evaluator = new DiceEvaluator(new MockRandomNumberGenerator(new List<int> { 6, 6, 2 }));
            var modifiers = new List<Modifier> { new CompoundingModifier(ComparisonOperator.GreaterThan, 5) };
            var basicRoll = new BasicRoll(1, new DieType.Basic(6), modifiers);
            Assert.AreEqual(14, evaluator.Evaluate(basicRoll));
        }

        [Test]
        public void Evaluate_BasicRoll_WithSuccessModifier()
        {
            var evaluator = new DiceEvaluator(new MockRandomNumberGenerator(new List<int> { 5, 2, 6 }));
            var modifiers = new List<Modifier> { new SuccessModifier(ComparisonOperator.GreaterThan, 4) };
            var basicRoll = new BasicRoll(3, new DieType.Basic(6), modifiers);
            Assert.AreEqual(2, evaluator.Evaluate(basicRoll));
        }

        [Test]
        public void Evaluate_BasicRoll_WithFailureModifier()
        {
            var evaluator = new DiceEvaluator(new MockRandomNumberGenerator(new List<int> { 5, 2, 6 }));
            var modifiers = new List<Modifier> { new FailureModifier(ComparisonOperator.LessThan, 3) };
            var basicRoll = new BasicRoll(3, new DieType.Basic(6), modifiers);
            Assert.AreEqual(-1, evaluator.Evaluate(basicRoll));
        }

        [Theory]
        [TestCaseSource(nameof(Evaluate_BasicRoll_WithConstantModifier_TestCases))]
        public void Evaluate_BasicRoll_WithConstantModifier(List<int> numbers, List<Modifier> mods, int expected)
        {
            var evaluator = new DiceEvaluator(new MockRandomNumberGenerator(numbers));
            var basicRoll = new BasicRoll(numbers.Count, new DieType.Basic(6), mods);
            Assert.AreEqual(expected, evaluator.Evaluate(basicRoll));
        }

        public static IEnumerable<TestCaseData> Evaluate_BasicRoll_WithConstantModifier_TestCases()
        {
            yield return new TestCaseData(new List<int> { 5, 2 }, new List<Modifier> { new ConstantModifier(ArithmaticOperator.Add, 3) }, 10);
            yield return new TestCaseData(new List<int> { 5, 2 }, new List<Modifier> { new ConstantModifier(ArithmaticOperator.Subtract, 3) }, 4);
        }

        [Theory]
        [TestCaseSource(nameof(Evaluate_BasicRoll_WithConstantModifierAndAnotherModifier_TestCases))]
         public void Evaluate_BasicRoll_WithConstantModifierAndAnotherModifier(List<int> numbers, List<Modifier> mods, int expected)
        {
            var evaluator = new DiceEvaluator(new MockRandomNumberGenerator(numbers));
            var modifiers = mods;
            var basicRoll = new BasicRoll(numbers.Count, new DieType.Basic(6), modifiers);
            Assert.AreEqual(expected, evaluator.Evaluate(basicRoll));
        }

        public static IEnumerable<TestCaseData> Evaluate_BasicRoll_WithConstantModifierAndAnotherModifier_TestCases()
        {
            yield return new TestCaseData(new List<int> { 5, 2 }, new List<Modifier> { new KeepModifier(1, true), new ConstantModifier(ArithmaticOperator.Add, 6) }, 11);
            yield return new TestCaseData(new List<int> { 5, 2 }, new List<Modifier> { new ConstantModifier(ArithmaticOperator.Add, 6), new KeepModifier(1, true) }, 6);
        }

        [Test]
        public void Evaluate_UnknownRollType_ThrowsArgumentException()
        {
            var evaluator = new DiceEvaluator();
            var unknownRoll = new UnknownRoll();
            Assert.Throws<ArgumentException>(() => evaluator.Evaluate(unknownRoll));
        }

        // Mock class for testing purposes
        private record UnknownRoll : Roll { }

        // Mock Random Number Generator for testing purposes
        private class MockRandomNumberGenerator : IRandomNumberGenerator<int>
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