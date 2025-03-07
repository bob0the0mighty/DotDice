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

        [Theory]
        [TestCaseSource(nameof(Evaluate_BasicRoll_WithRerollOnceModifier_TestCases))]
        public void Evaluate_BasicRoll_WithRerollOnceModifier(List<int> numbers, int numberOfDice, DieType dieType, ComparisonOperator comparisonOperator, int value, int expected)
        {
            var evaluator = new DiceEvaluator(new MockRandomNumberGenerator(numbers));
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
            var evaluator = new DiceEvaluator(new MockRandomNumberGenerator(numbers));
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
            yield return new TestCaseData(new MockRandomNumberGenerator(new List<int> { 6, 6, 2 }), 1, new DieType.Basic(6), ComparisonOperator.GreaterThan, 5, 14);
            yield return new TestCaseData(new MockRandomNumberGenerator(new List<int> { 4, 4, 2 }), 1, new DieType.Basic(4), ComparisonOperator.Equal, 4, 10);
            yield return new TestCaseData(new MockRandomNumberGenerator(new List<int> { 8, 7, 6 }), 1, new DieType.Basic(8), ComparisonOperator.GreaterThan, 6, 21);
            yield return new TestCaseData(new MockRandomNumberGenerator(new List<int> { 10, 8 }), 1, new DieType.Basic(10), ComparisonOperator.GreaterThan, 9, 18);
            yield return new TestCaseData(new MockRandomNumberGenerator(new List<int> { 20, 15 }), 1, new DieType.Basic(20), ComparisonOperator.Equal, 20, 35);
            // Multiple dice tests
            yield return new TestCaseData(new MockRandomNumberGenerator(new List<int> { 5, 6, 6, 3 }), 2, new DieType.Basic(6), ComparisonOperator.GreaterThan, 5, 20);
            yield return new TestCaseData(new MockRandomNumberGenerator(new List<int> { 8, 3, 8, 8, 2 }), 2, new DieType.Basic(8), ComparisonOperator.Equal, 8, 29);
            yield return new TestCaseData(new MockRandomNumberGenerator(new List<int> { 4, 2, 3, 4, 4, 2 }), 3, new DieType.Basic(4), ComparisonOperator.GreaterThan, 3, 19);
            // Different die types
            yield return new TestCaseData(new MockRandomNumberGenerator(new List<int> { 12, 12, 5 }), 1, new DieType.Basic(12), ComparisonOperator.GreaterThan, 11, 29);
            yield return new TestCaseData(new MockRandomNumberGenerator(new List<int> { 100, 50 }), 1, new DieType.Percent(), ComparisonOperator.Equal, 100, 150);
            yield return new TestCaseData(new MockRandomNumberGenerator(new List<int> { 1, 1, -1 }), 1, new DieType.Fudge(), ComparisonOperator.Equal, 1, 1);
            // Mixed scenarios
            yield return new TestCaseData(new MockRandomNumberGenerator(new List<int> { 7, 9, 10, 4 }), 2, new DieType.Basic(10), ComparisonOperator.GreaterThan, 8, 30);
            yield return new TestCaseData(new MockRandomNumberGenerator(new List<int> { 3, 3, 3, 2 }), 1, new DieType.Basic(3), ComparisonOperator.GreaterThan, 2, 11);
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
            yield return new TestCaseData(new MockRandomNumberGenerator(new List<int> { 6, 6, 2 }), 1, new DieType.Basic(6), ComparisonOperator.GreaterThan, 5, 14);
            yield return new TestCaseData(new MockRandomNumberGenerator(new List<int> { 4, 4, 2 }), 1, new DieType.Basic(4), ComparisonOperator.Equal, 4, 10);
            yield return new TestCaseData(new MockRandomNumberGenerator(new List<int> { 8, 7, 6 }), 1, new DieType.Basic(8), ComparisonOperator.GreaterThan, 6, 21);
            yield return new TestCaseData(new MockRandomNumberGenerator(new List<int> { 10, 8 }), 1, new DieType.Basic(10), ComparisonOperator.GreaterThan, 9, 18);
            yield return new TestCaseData(new MockRandomNumberGenerator(new List<int> { 20, 15 }), 1, new DieType.Basic(20), ComparisonOperator.Equal, 20, 35);
            // Multiple dice tests (each die is evaluated separately for compounding)
            yield return new TestCaseData(new MockRandomNumberGenerator(new List<int> { 6, 3, 6, 4 }), 2, new DieType.Basic(6), ComparisonOperator.GreaterThan, 5, 19);
            yield return new TestCaseData(new MockRandomNumberGenerator(new List<int> { 8, 5, 8, 7 }), 2, new DieType.Basic(8), ComparisonOperator.Equal, 8, 28);
            yield return new TestCaseData(new MockRandomNumberGenerator(new List<int> { 4, 3, 2, 4, 4, 1 }), 3, new DieType.Basic(4), ComparisonOperator.GreaterThan, 3, 18);
            // Different die types
            yield return new TestCaseData(new MockRandomNumberGenerator(new List<int> { 12, 12, 5 }), 1, new DieType.Basic(12), ComparisonOperator.GreaterThan, 11, 29);
            yield return new TestCaseData(new MockRandomNumberGenerator(new List<int> { 100, 50 }), 1, new DieType.Percent(), ComparisonOperator.Equal, 100, 150);
            yield return new TestCaseData(new MockRandomNumberGenerator(new List<int> { 1, 1, -1 }), 1, new DieType.Fudge(), ComparisonOperator.Equal, 1, 1);
            // Mixed scenarios
            yield return new TestCaseData(new MockRandomNumberGenerator(new List<int> { 10, 10, 10, 2, 5 }), 2, new DieType.Basic(10), ComparisonOperator.GreaterThan, 9, 37);
            yield return new TestCaseData(new MockRandomNumberGenerator(new List<int> { 3, 3, 3, 3, 1 }), 1, new DieType.Basic(3), ComparisonOperator.GreaterThan, 2, 13);
            // Max compound tests (with repeating generator)
            yield return new TestCaseData(new RepeatingRandomNumberGenerator(6), 1, new DieType.Basic(6), ComparisonOperator.GreaterThan, 5, 606); // Will hit compound limit of 100
            yield return new TestCaseData(new RepeatingRandomNumberGenerator(20), 1, new DieType.Basic(20), ComparisonOperator.Equal, 20, 2020); // Will hit compound limit of 100
        }

        [Theory]
        [TestCaseSource(nameof(Evaluate_BasicRoll_WithSuccessModifier_TestCases))]
        public void Evaluate_BasicRoll_WithSuccessModifier(List<int> numbers, int numberOfDice, DieType dieType, ComparisonOperator comparisonOperator, int value, int expected)
        {
            var evaluator = new DiceEvaluator(new MockRandomNumberGenerator(numbers));
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
            var evaluator = new DiceEvaluator(new MockRandomNumberGenerator(numbers));
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
            yield return new TestCaseData(new List<int> { 5, 2 }, new List<Modifier> { new ConstantModifier(ArithmaticOperator.Add, 6), new KeepModifier(1, true) }, 11);
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

        // Special Random Number Generator that returns the same value repeatedly
        private class RepeatingRandomNumberGenerator : IRandomNumberGenerator<int>
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
    }
}