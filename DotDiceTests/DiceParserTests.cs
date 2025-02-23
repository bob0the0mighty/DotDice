using NUnit.Framework;
using DotDice;
using System.Collections.Generic;
using Pidgin;

namespace DotDice.Tests
{
    [TestFixture]
    public class DiceParserTestsTest
    {
        [TestCase("=5", ComparisonOperator.Equal, 5)]
        [TestCase(">10", ComparisonOperator.GreaterThan, 10)]
        [TestCase("<3", ComparisonOperator.LessThan, 3)]
        public void SuccessModifier_Parse_ShouldReturnExpectedModifier(string input, ComparisonOperator expectedOp, int expectedValue)
        {
            var result = DiceParser.successModifier.Parse(input);
            var modifier = result.Value as SuccessModifier;
            Assert.NotNull(modifier);
            Assert.That(modifier.Operator, Is.EqualTo(expectedOp));
            Assert.That(modifier.Value, Is.EqualTo(expectedValue));
        }

        [TestCase("f=5", ComparisonOperator.Equal, 5)]
        [TestCase("f>10", ComparisonOperator.GreaterThan, 10)]
        [TestCase("f<3", ComparisonOperator.LessThan, 3)]
        public void FailureModifier_Parse_ShouldReturnExpectedModifier(string input, ComparisonOperator expectedOp, int expectedValue)
        {
            var result = DiceParser.failureModifier.Parse(input);
            var modifier = result.Value as FailureModifier;
            Assert.NotNull(modifier);
            Assert.That(modifier.Operator, Is.EqualTo(expectedOp));
            Assert.That(modifier.Value, Is.EqualTo(expectedValue));
        }

        [TestCase("!5", ComparisonOperator.Equal, 5)]
        [TestCase("!>10", ComparisonOperator.GreaterThan, 10)]
        [TestCase("!<3", ComparisonOperator.LessThan, 3)]
        public void ExplodeModifier_Parse_ShouldReturnExpectedModifier(string input, ComparisonOperator expectedOp, int expectedValue)
        {
            var result = DiceParser.explodeModifier.Parse(input);
            var modifier = result.Value as ExplodeModifier;
            Assert.NotNull(modifier);
            Assert.That(modifier.Operator, Is.EqualTo(expectedOp));
            Assert.That(modifier.Value, Is.EqualTo(expectedValue));
        }

        [TestCase("^5", ComparisonOperator.Equal, 5)]
        [TestCase("^>10", ComparisonOperator.GreaterThan, 10)]
        [TestCase("^<3", ComparisonOperator.LessThan, 3)]
        public void CompoundingModifier_Parse_ShouldReturnExpectedModifier(string input, ComparisonOperator expectedOp, int expectedValue)
        {
            var result = DiceParser.compoundingModifier.Parse(input);
            var modifier = result.Value as CompoundingModifier;
            Assert.NotNull(modifier);
            Assert.That(modifier.Operator, Is.EqualTo(expectedOp));
            Assert.That(modifier.Value, Is.EqualTo(expectedValue));
        }

        [TestCase("ro=5", ComparisonOperator.Equal, 5, true)]
        [TestCase("ro>10", ComparisonOperator.GreaterThan, 10, false)]
        public void RerollModifier_Parse_ShouldReturnExpectedModifier(string input, ComparisonOperator expectedOp, int expectedValue, bool expectedOnlyOnce)
        {
            var result = DiceParser.rerollOnceModifier.Parse(input);
            var modifier = result.Value as RerollOnceModifier;
            Assert.NotNull(modifier);
            Assert.That(modifier.Operator, Is.EqualTo(expectedOp));
            Assert.That(modifier.Value, Is.EqualTo(expectedValue));
        }

        [TestCase("rc=5", ComparisonOperator.Equal, 5)]
        [TestCase("rc>10", ComparisonOperator.GreaterThan, 10)]
        public void RerollModifier_Parse_ShouldReturnExpectedModifier(string input, ComparisonOperator expectedOp, int expectedValue)
        {
            var result = DiceParser.rerollCompoundModifier.Parse(input);
            var modifier = result.Value as RerollCompoundModifier;
            Assert.NotNull(modifier);
            Assert.That(modifier.Operator, Is.EqualTo(expectedOp));
            Assert.That(modifier.Value, Is.EqualTo(expectedValue));
        }

        [TestCase("kh", 1, true)]
        [TestCase("kh3", 3, true)]
        public void KeephIGHModifier_Parse_ShouldReturnExpectedKeepModifier(string input, int expectedCount, bool expectedKeepHighest)
        {
            var result = DiceParser.keepHighModifier.Parse(input);
            var modifier = result.Value as KeepModifier;
            Assert.NotNull(modifier);
            Assert.That(modifier.Count, Is.EqualTo(expectedCount));
            Assert.That(modifier.KeepHighest, Is.EqualTo(expectedKeepHighest));
        }

        [TestCase("kl", 1, false)]
        [TestCase("kl2", 2, false)]
        public void KeepLowModifier_Parse_ShouldReturnExpectedKeepModifier(string input, int expectedCount, bool expectedKeepHighest)
        {
            var result = DiceParser.keepLowModifier.Parse(input);
            var modifier = result.Value as KeepModifier;
            Assert.NotNull(modifier);
            Assert.That(modifier.Count, Is.EqualTo(expectedCount));
            Assert.That(modifier.KeepHighest, Is.EqualTo(expectedKeepHighest));
        }

        [TestCase("dh", 1, true)]
        [TestCase("dh3", 3, true)]
        public void DropHighModifier_Parse_ShouldReturnExpectedDropModifier(string input, int expectedCount, bool expectedDropHighest)
        {
            var result = DiceParser.dropHighModifier.Parse(input);
            var modifier = result.Value as DropModifier;
            Assert.NotNull(modifier);
            Assert.That(modifier.Count, Is.EqualTo(expectedCount));
            Assert.That(modifier.DropHighest, Is.EqualTo(expectedDropHighest));
        }

        [TestCase("dl", 1, false)]
        [TestCase("dl2", 2, false)]
        public void DropLowModifier_Parse_ShouldReturnExpectedDropModifier(string input, int expectedCount, bool expectedDropHighest)
        {
            var result = DiceParser.dropLowModifier.Parse(input);
            var modifier = result.Value as DropModifier;
            Assert.NotNull(modifier);
            Assert.That(modifier.Count, Is.EqualTo(expectedCount));
            Assert.That(modifier.DropHighest, Is.EqualTo(expectedDropHighest));
        }

        [TestCase("5", 5)]
        [TestCase("10", 10)]
        public void Constant_Parse_ShouldReturnExpectedConstant(string input, int expectedValue)
        {
            var result = DiceParser.Constant.Parse(input);
            var constant = result.Value as Constant;
            Assert.NotNull(constant);
            Assert.That(constant.Value, Is.EqualTo(expectedValue));
        }

        public static IEnumerable<TestCaseData> BasicRollTestCases
        {
            get
            {
                yield return new TestCaseData("d6", 1, new DieType.Basic(6));
                yield return new TestCaseData("2d10", 2, new DieType.Basic(10));
                yield return new TestCaseData("dF", 1, new DieType.Fudge());
                yield return new TestCaseData("d%", 1, new DieType.Percent());
            }
        }

        [TestCaseSource(nameof(BasicRollTestCases))]
        public void BasicRoll_Parse_ShouldReturnExpectedBasicRoll(string input, int expectedNumDice, DieType expectedDieType)
        {
            var result = DiceParser.basicRoll.Parse(input);
            var roll = result.Value as BasicRoll;
            Assert.NotNull(roll);
            Assert.That(roll.NumberOfDice, Is.EqualTo(expectedNumDice));
            Assert.That(roll.DieType, Is.EqualTo(expectedDieType));
        }
    }
}