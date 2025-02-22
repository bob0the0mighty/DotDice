using NUnit.Framework;
using DotDice;
using System.Collections.Generic;

namespace DotDice.Tests
{
    [TestFixture]
    public class DiceParserTests
    {
        [TestCase("=5", ComparisonOperator.Equal, 5)]
        [TestCase(">10", ComparisonOperator.GreaterThan, 10)]
        [TestCase("<3", ComparisonOperator.LessThan, 3)]
        public void SuccessModifier_ParsesCorrectly(string input, ComparisonOperator expectedOp, int expectedValue)
        {
            var result = DiceParser.successModifier.Run(input);
            var successModifier = result.Value as SuccessModifier;
            Assert.IsNotNull(successModifier);
            Assert.AreEqual(expectedOp, successModifier.Operator);
            Assert.AreEqual(expectedValue, successModifier.Value);
        }

        [TestCase("f=5", ComparisonOperator.Equal, 5)]
        [TestCase("f>10", ComparisonOperator.GreaterThan, 10)]
        [TestCase("f<3", ComparisonOperator.LessThan, 3)]
        public void FailureModifier_ParsesCorrectly(string input, ComparisonOperator expectedOp, int expectedValue)
        {
            var result = DiceParser.failureModifier.Run(input);
            var failureModifier = result.Value as FailureModifier;
            Assert.AreEqual(expectedOp, failureModifier.Operator);
            Assert.AreEqual(expectedValue, failureModifier.Value);
        }

        [TestCase("!5", ComparisonOperator.Equal, 5)]
        [TestCase("!>10", ComparisonOperator.GreaterThan, 10)]
        [TestCase("!<3", ComparisonOperator.LessThan, 3)]
        public void ExplodeModifier_ParsesCorrectly(string input, ComparisonOperator expectedOp, int expectedValue)
        {
            var result = DiceParser.explodeModifier.Run(input);
            var explodeModifier = (ExplodeModifier)result.Value;
            Assert.AreEqual(expectedOp, explodeModifier.Operator);
            Assert.AreEqual(expectedValue, explodeModifier.Value);
        }

        [TestCase("!!5", ComparisonOperator.Equal, 5)]
        [TestCase("!!>10", ComparisonOperator.GreaterThan, 10)]
        [TestCase("!!<3", ComparisonOperator.LessThan, 3)]
        public void CompoundingModifier_ParsesCorrectly(string input, ComparisonOperator expectedOp, int expectedValue)
        {
            var result = DiceParser.compoundingModifier.Run(input);
            var compoundingModifier = (CompoundingModifier)result.Value;
            Assert.AreEqual(expectedOp, compoundingModifier.Operator);
            Assert.AreEqual(expectedValue, compoundingModifier.Value);
        }

        [TestCase("ro=5", ComparisonOperator.Equal, 5, true)]
        [TestCase("r>10", ComparisonOperator.GreaterThan, 10, false)]
        public void RerollModifier_ParsesCorrectly(string input, ComparisonOperator expectedOp, int expectedValue, bool expectedOnlyOnce)
        {
            var result = DiceParser.rerollModifier.Run(input);
            var rerollModifier = (RerollModifier)result.Value;
            Assert.AreEqual(expectedOp, rerollModifier.Operator);
            Assert.AreEqual(expectedValue, rerollModifier.Value);
            Assert.AreEqual(expectedOnlyOnce, rerollModifier.OnlyOnce);
        }

        [TestCase("kh", 1, true)]
        [TestCase("kh3", 3, true)]
        [TestCase("kl", 1, false)]
        [TestCase("kl2", 2, false)]
        public void KeepHighestModifier_ParsesCorrectly(string input, int expectedCount, bool expectedHighest)
        {
            var result = DiceParser.keepModifier.Run(input);
            var keepModifier = (KeepModifier)result.Value;
            Assert.AreEqual(expectedCount, keepModifier.Count);
            Assert.AreEqual(expectedHighest, keepModifier.KeepHighest);
        }

        [TestCase("dh", 1, false)]
        [TestCase("dh3", 3, false)]
        [TestCase("dl", 1, true)]
        [TestCase("dl2", 2, true)]
        public void DropModifier_ParsesCorrectly(string input, int expectedCount, bool expectedLowest)
        {
            var result = DiceParser.dropModifier.Run(input);
            var dropModifier = (DropModifier)result.Value;
            Assert.AreEqual(expectedCount, dropModifier.Count);
            Assert.AreEqual(expectedLowest, dropModifier.DropLowest);
        }

        [TestCase("5", 5)]
        [TestCase("10", 10)]
        public void Constant_ParsesCorrectly(string input, int expectedValue)
        {
            var result = DiceParser.Constant.Run(input);
            var constant = (Constant)result.Value;
            Assert.AreEqual(expectedValue, constant.Value);
        }

        [TestCase("d6", 1, 6, false, false)]
        [TestCase("2d10", 2, 10, false, false)]
        [TestCase("dF", 1, 0, true, false)]
        [TestCase("d%", 1, 0, false, true)]
        public void BasicRoll_ParsesCorrectly(string input, int expectedNumDice, int expectedDieType, bool expectedIsFudge, bool expectedIsPercentile)
        {
            var result = DiceParser.basicRoll.Run(input);
            var basicRoll = (BasicRoll)result.Value;
            Assert.AreEqual(expectedNumDice, basicRoll.NumberOfDice);
            Assert.AreEqual(expectedDieType, basicRoll.DieType);
            Assert.AreEqual(expectedIsFudge, basicRoll.IsFudgeDie);
            Assert.AreEqual(expectedIsPercentile, basicRoll.IsPercentile);
        }

        [TestCase("{d6,d10}", new[] { 6, 10 })]
        [TestCase("{dF,d%}", new[] { 0, 0 })]
        public void GroupedRoll_ParsesCorrectly(string input, int[] expectedDieTypes)
        {
            var result = DiceParser.groupedRoll.Run(input);
            var groupedRoll = (GroupedRoll)result.Value;
            Assert.AreEqual(expectedDieTypes.Length, groupedRoll.Rolls.Count);
            for (int i = 0; i < expectedDieTypes.Length; i++)
            {
                var basicRoll = (BasicRoll)groupedRoll.Rolls[i];
                Assert.AreEqual(expectedDieTypes[i], basicRoll.DieType);
            }
        }
    }
}