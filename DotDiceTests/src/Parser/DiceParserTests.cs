using DotDice.Parser;
using Pidgin;

namespace DotDice.Tests
{
    [TestFixture]
    public class DiceParserTests
    {
        #region SuccessModifier Tests

        [TestCase("=5", ComparisonOperator.Equal, 5)]
        [TestCase(">10", ComparisonOperator.GreaterThan, 10)]
        [TestCase("<3", ComparisonOperator.LessThan, 3)]
        public void SuccessModifier_Parse_ValidInput_ShouldSucceed(string input, ComparisonOperator expectedOp, int expectedValue)
        {
            var result = DiceParser.successModifier.Parse(input);
            Assert.IsTrue(result.Success, "Parser should succeed for valid input");
            var modifier = result.Value as SuccessModifier;
            Assert.NotNull(modifier);
            Assert.That(modifier.Operator, Is.EqualTo(expectedOp));
            Assert.That(modifier.Value, Is.EqualTo(expectedValue));
        }

        [TestCase("5")] // Missing operator
        [TestCase("=>5")] // Invalid operator syntax
        [TestCase("=a")] // Non-numeric value
        public void SuccessModifier_Parse_InvalidInput_ShouldFail(string input)
        {
            var result = DiceParser.successModifier.Parse(input);
            Assert.IsFalse(result.Success, "Parser should fail for invalid input");
        }

        #endregion

        #region FailureModifier Tests

        [TestCase("f=5", ComparisonOperator.Equal, 5)]
        [TestCase("f>10", ComparisonOperator.GreaterThan, 10)]
        [TestCase("f<3", ComparisonOperator.LessThan, 3)]
        public void FailureModifier_Parse_ValidInput_ShouldSucceed(string input, ComparisonOperator expectedOp, int expectedValue)
        {
            var result = DiceParser.failureModifier.Parse(input);
            Assert.IsTrue(result.Success, "Parser should succeed for valid failure modifier");
            var modifier = result.Value as FailureModifier;
            Assert.NotNull(modifier);
            Assert.That(modifier.Operator, Is.EqualTo(expectedOp));
            Assert.That(modifier.Value, Is.EqualTo(expectedValue));
        }

        [TestCase("f5")] // Missing operator symbol
        [TestCase("f>")] // Missing value
        public void FailureModifier_Parse_InvalidInput_ShouldFail(string input)
        {
            var result = DiceParser.failureModifier.Parse(input);
            Assert.IsFalse(result.Success, "Parser should fail for invalid failure modifier");
        }

        #endregion

        #region ExplodeModifier Tests

        [TestCase("!=5", ComparisonOperator.Equal, 5)]
        [TestCase("!>10", ComparisonOperator.GreaterThan, 10)]
        [TestCase("!<3", ComparisonOperator.LessThan, 3)]
        public void ExplodeModifier_Parse_ValidInput_ShouldSucceed(string input, ComparisonOperator expectedOp, int expectedValue)
        {
            var result = DiceParser.explodeModifier.Parse(input);
            Assert.IsTrue(result.Success, "Parser should succeed for valid explode modifier");
            var modifier = result.Value as ExplodeModifier;
            Assert.NotNull(modifier);
            Assert.That(modifier.Operator, Is.EqualTo(expectedOp));
            Assert.That(modifier.Value, Is.EqualTo(expectedValue));
        }

        [TestCase("!5")]  // Missing operator
        [TestCase("!>")]  // Missing value
        public void ExplodeModifier_Parse_InvalidInput_ShouldFail(string input)
        {
            var result = DiceParser.explodeModifier.Parse(input);
            Assert.IsFalse(result.Success, "Parser should fail for invalid explode modifier");
        }

        #endregion

        #region CompoundingModifier Tests

        [TestCase("^=5", ComparisonOperator.Equal, 5)]
        [TestCase("^>10", ComparisonOperator.GreaterThan, 10)]
        [TestCase("^<3", ComparisonOperator.LessThan, 3)]
        public void CompoundingModifier_Parse_ValidInput_ShouldSucceed(string input, ComparisonOperator expectedOp, int expectedValue)
        {
            var result = DiceParser.compoundingModifier.Parse(input);
            Assert.IsTrue(result.Success, "Parser should succeed for valid compounding modifier");
            var modifier = result.Value as CompoundingModifier;
            Assert.NotNull(modifier);
            Assert.That(modifier.Operator, Is.EqualTo(expectedOp));
            Assert.That(modifier.Value, Is.EqualTo(expectedValue));
        }

        [TestCase("^5")]  // Missing operator
        [TestCase("^>")]  // Missing value
        public void CompoundingModifier_Parse_InvalidInput_ShouldFail(string input)
        {
            var result = DiceParser.compoundingModifier.Parse(input);
            Assert.IsFalse(result.Success, "Parser should fail for invalid compounding modifier");
        }

        #endregion

        #region Reroll Once Modifier Tests

        [TestCase("ro=5", ComparisonOperator.Equal, 5, true)]
        [TestCase("ro>10", ComparisonOperator.GreaterThan, 10, false)]
        public void RerollOnceModifier_Parse_ValidInput_ShouldSucceed(string input, ComparisonOperator expectedOp, int expectedValue, bool expectedOnlyOnce)
        {
            var result = DiceParser.rerollOnceModifier.Parse(input);
            Assert.IsTrue(result.Success, "Parser should succeed for valid reroll once modifier");
            var modifier = result.Value as RerollOnceModifier;
            Assert.NotNull(modifier);
            Assert.That(modifier.Operator, Is.EqualTo(expectedOp));
            Assert.That(modifier.Value, Is.EqualTo(expectedValue));
        }

        [TestCase("ro5")]  // Missing operator
        [TestCase("ro>")]  // Missing value
        public void RerollOnceModifier_Parse_InvalidInput_ShouldFail(string input)
        {
            var result = DiceParser.rerollOnceModifier.Parse(input);
            Assert.IsFalse(result.Success, "Parser should fail for invalid reroll once modifier");
        }

        #endregion

        #region Reroll Compound Modifier Tests

        [TestCase("rc=5", ComparisonOperator.Equal, 5)]
        [TestCase("rc>10", ComparisonOperator.GreaterThan, 10)]
        public void RerollCompoundModifier_Parse_ValidInput_ShouldSucceed(string input, ComparisonOperator expectedOp, int expectedValue)
        {
            var result = DiceParser.rerollCompoundModifier.Parse(input);
            Assert.IsTrue(result.Success, "Parser should succeed for valid reroll compound modifier");
            var modifier = result.Value as RerollMultipleModifier;
            Assert.NotNull(modifier);
            Assert.That(modifier.Operator, Is.EqualTo(expectedOp));
            Assert.That(modifier.Value, Is.EqualTo(expectedValue));
        }

        [TestCase("rc5")]   // Missing operator
        [TestCase("rc>")]   // Missing value
        public void RerollCompoundModifier_Parse_InvalidInput_ShouldFail(string input)
        {
            var result = DiceParser.rerollCompoundModifier.Parse(input);
            Assert.IsFalse(result.Success, "Parser should fail for invalid reroll compound modifier");
        }

        #endregion

        #region Keep Modifier Tests

        [TestCase("kh", 1, true)]
        [TestCase("kh3", 3, true)]
        public void KeepHighModifier_Parse_ValidInput_ShouldSucceed(string input, int expectedCount, bool expectedKeepHighest)
        {
            var result = DiceParser.keepHighModifier.Parse(input);
            Assert.IsTrue(result.Success, "Parser should succeed for valid keep high modifier");
            var modifier = result.Value as KeepModifier;
            Assert.NotNull(modifier);
            Assert.That(modifier.Count, Is.EqualTo(expectedCount));
            Assert.That(modifier.KeepHighest, Is.EqualTo(expectedKeepHighest));
        }

        [TestCase("kh0")]  // Zero count not valid if assumed positive
        [TestCase("kh-1")] // Negative count
        public void KeepHighModifier_Parse_InvalidInput_ShouldFail(string input)
        {
            var result = DiceParser.keepHighModifier.Parse(input);
            Assert.IsFalse(result.Success, "Parser should fail for invalid keep high modifier count");
        }

        [TestCase("kl", 1, false)]
        [TestCase("kl2", 2, false)]
        public void KeepLowModifier_Parse_ValidInput_ShouldSucceed(string input, int expectedCount, bool expectedKeepHighest)
        {
            var result = DiceParser.keepLowModifier.Parse(input);
            Assert.IsTrue(result.Success, "Parser should succeed for valid keep low modifier");
            var modifier = result.Value as KeepModifier;
            Assert.NotNull(modifier);
            Assert.That(modifier.Count, Is.EqualTo(expectedCount));
            Assert.That(modifier.KeepHighest, Is.EqualTo(expectedKeepHighest));
        }

        [TestCase("kl0")]  // Zero count not acceptable
        [TestCase("kl-2")] // Negative count
        public void KeepLowModifier_Parse_InvalidInput_ShouldFail(string input)
        {
            var result = DiceParser.keepLowModifier.Parse(input);
            Assert.IsFalse(result.Success, "Parser should fail for invalid keep low modifier count");
        }

        #endregion

        #region Drop Modifier Tests

        [TestCase("dh", 1, true)]
        [TestCase("dh3", 3, true)]
        public void DropHighModifier_Parse_ValidInput_ShouldSucceed(string input, int expectedCount, bool expectedDropHighest)
        {
            var result = DiceParser.dropHighModifier.Parse(input);
            Assert.IsTrue(result.Success, "Parser should succeed for valid drop high modifier");
            var modifier = result.Value as DropModifier;
            Assert.NotNull(modifier);
            Assert.That(modifier.Count, Is.EqualTo(expectedCount));
            Assert.That(modifier.DropHighest, Is.EqualTo(expectedDropHighest));
        }

        [TestCase("dh0")]  // Zero count not acceptable if rule enforced
        [TestCase("dh-1")] // Negative count
        public void DropHighModifier_Parse_InvalidInput_ShouldFail(string input)
        {
            var result = DiceParser.dropHighModifier.Parse(input);
            Assert.IsFalse(result.Success, "Parser should fail for invalid drop high modifier count");
        }

        [TestCase("dl", 1, false)]
        [TestCase("dl2", 2, false)]
        public void DropLowModifier_Parse_ValidInput_ShouldSucceed(string input, int expectedCount, bool expectedDropHighest)
        {
            var result = DiceParser.dropLowModifier.Parse(input);
            Assert.IsTrue(result.Success, "Parser should succeed for valid drop low modifier");
            var modifier = result.Value as DropModifier;
            Assert.NotNull(modifier);
            Assert.That(modifier.Count, Is.EqualTo(expectedCount));
            Assert.That(modifier.DropHighest, Is.EqualTo(expectedDropHighest));
        }

        [TestCase("dl0")]
        [TestCase("dl-2")]
        public void DropLowModifier_Parse_InvalidInput_ShouldFail(string input)
        {
            var result = DiceParser.dropLowModifier.Parse(input);
            Assert.IsFalse(result.Success, "Parser should fail for invalid drop low modifier count");
        }

        #endregion

        #region Constant Tests

        [TestCase("5", 5)]
        [TestCase("10", 10)]
        public void Constant_Parse_ValidInput_ShouldSucceed(string input, int expectedValue)
        {
            var result = DiceParser.Constant.Parse(input);
            Assert.IsTrue(result.Success, "Parser should succeed for valid constant input");
            var constant = result.Value as Constant;
            Assert.NotNull(constant);
            Assert.That(constant.Value, Is.EqualTo(expectedValue));
        }

        [TestCase("a")]
        [TestCase("")]
        [TestCase("5a")]
        public void Constant_Parse_InvalidInput_ShouldFail(string input)
        {
            var result = DiceParser.Constant.Parse(input);
            Assert.IsFalse(result.Success, "Parser should fail for invalid constant input");
        }

        #endregion

        #region ConstantModifier Tests

        [TestCase("+5", 5)]
        [TestCase("-15", 15)]//because the value should always br positive.
        [TestCase("+99", 99)]
        public void ConstantModifier_Parse_ValidInput_ShouldSucceed(string input, int expectedValue)
        {
            var result = DiceParser.constantModifier.Parse(input);
            Assert.IsTrue(result.Success, "Parser should succeed for valid constant modifier input");
            var modifier = result.Value as ConstantModifier;
            Assert.NotNull(modifier);
            Assert.That(modifier.Value, Is.EqualTo(expectedValue));
        }

        [TestCase("5")]      // Missing sign
        [TestCase("+0")]     // Zero is not allowed
        [TestCase("-0")]     // Zero is not allowed
        [TestCase("+a")]     // Non-numeric value
        public void ConstantModifier_Parse_InvalidInput_ShouldFail(string input)
        {
            var result = DiceParser.constantModifier.Parse(input);
            Assert.IsFalse(result.Success, "Parser should fail for invalid constant modifier input");
        }

        #endregion

        #region Basic Roll Tests

        public static IEnumerable<TestCaseData> BasicRollTestCases
        {
            get
            {
                yield return new TestCaseData("d6", 1, new DieType.Basic(6), true);
                yield return new TestCaseData("2d10", 2, new DieType.Basic(10), true);
                yield return new TestCaseData("2d10+5", 2, new DieType.Basic(10), true);
                yield return new TestCaseData("2d10-5", 2, new DieType.Basic(10), true);
                yield return new TestCaseData("dF", 1, new DieType.Fudge(), true);
                yield return new TestCaseData("d%", 1, new DieType.Percent(), true);
                yield return new TestCaseData("2d20>15", 2, new DieType.Basic(20), true);
                yield return new TestCaseData("2d20f<15", 2, new DieType.Basic(20), true);
                yield return new TestCaseData("3d6!>3", 3, new DieType.Basic(6), true);
                yield return new TestCaseData("4d8^<2", 4, new DieType.Basic(8), true);
                yield return new TestCaseData("d10ro=5", 1, new DieType.Basic(10), true);
                yield return new TestCaseData("d12rc>3", 1, new DieType.Basic(12), true);
                yield return new TestCaseData("5d6kh3", 5, new DieType.Basic(6), true);
                yield return new TestCaseData("2d6kh", 2, new DieType.Basic(6), true);
                yield return new TestCaseData("2d6kl", 2, new DieType.Basic(6), true);
                yield return new TestCaseData("5d6kl2", 5, new DieType.Basic(6), true);
                yield return new TestCaseData("2d6dh2", 2, new DieType.Basic(6), true);
                yield return new TestCaseData("2d6dl", 2, new DieType.Basic(6), true);
                yield return new TestCaseData("10d10>15ro<2", 10, new DieType.Basic(10), true);
                yield return new TestCaseData("4d8ro=2kh3dl1", 4, new DieType.Basic(8), true);
                yield return new TestCaseData("8d6dl1ro=3kh5", 8, new DieType.Basic(6), true);
                yield return new TestCaseData("10d4ro=2kh3", 10, new DieType.Basic(4), true);
                yield return new TestCaseData("10d4ro=2kh3+20", 10, new DieType.Basic(4), true);
            }
        }

        [TestCaseSource(nameof(BasicRollTestCases))]
        public void BasicRoll_Parse_ValidInput_ShouldSucceed(
            string input,
            int expectedNumDice,
            DieType expectedDieType,
            bool expectedSuccess
        )
        {
            var result = DiceParser.basicRoll.Parse(input);
            Assert.That(result.Success, Is.EqualTo(expectedSuccess), $"Basic roll parsing failed for input '{input}'");
            var roll = result.Value as BasicRoll;
            Assert.NotNull(roll);
            Assert.That(roll.NumberOfDice, Is.EqualTo(expectedNumDice));
            Assert.That(roll.DieType, Is.EqualTo(expectedDieType));
        }

        [TestCase("d")]       // Missing die size
        [TestCase("3d")]      // Missing die size
        [TestCase("dd6")]     // Invalid format
        [TestCase("3d6abc")]  // Extra invalid characters
        public void BasicRoll_Parse_InvalidInput_ShouldFail(string input)
        {
            var result = DiceParser.basicRoll.Parse(input);
            Assert.IsFalse(result.Success, $"Basic roll parser should fail for input '{input}'");
        }

        #endregion
    }
}