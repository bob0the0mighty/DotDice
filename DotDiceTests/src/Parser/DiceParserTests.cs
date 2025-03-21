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
        [TestCase(" 1", 1)]
        [TestCase("99 ", 99)]
        [TestCase(" 93249 ", 93249)]
        public void Constant_Parse_ValidInput_ShouldSucceed(string input, int expectedValue)
        {
            var result = DiceParser.Constant.Parse(input);
            Assert.IsTrue(result.Success, "Parser should succeed for valid constant input");
            var constant = result.Value as Constant;
            Assert.NotNull(constant);
            Assert.That(constant.Value, Is.EqualTo(expectedValue));
        }

        [TestCase("a")]
        [TestCase("a3")]
        [TestCase("")]
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

        #region Multiple Modifiers Tests

        public static IEnumerable<TestCaseData> MultipleModifiersTestCases
        {
            get
            {
                yield return new TestCaseData("3d6>4f<2", 3, new DieType.Basic(6), 2);
                yield return new TestCaseData("2d20>15f<5", 2, new DieType.Basic(20), 2);
                yield return new TestCaseData("4d8!>7ro<2", 4, new DieType.Basic(8), 2);
                yield return new TestCaseData("5d10!>9rc<2", 5, new DieType.Basic(10), 2);
                yield return new TestCaseData("6d6kh3dl1", 6, new DieType.Basic(6), 2);
                yield return new TestCaseData("7d12kl2dh3", 7, new DieType.Basic(12), 2);
                yield return new TestCaseData("3d6>4kh2", 3, new DieType.Basic(6), 2);
                yield return new TestCaseData("4d8!>7kl3", 4, new DieType.Basic(8), 2);
                yield return new TestCaseData("5d10ro=1dh2", 5, new DieType.Basic(10), 2);
                yield return new TestCaseData("3d6>4f<2+5", 3, new DieType.Basic(6), 3);
                yield return new TestCaseData("4d8!>7ro<2-3", 4, new DieType.Basic(8), 3);
                yield return new TestCaseData("3d6>4f<2kh1dh1", 3, new DieType.Basic(6), 4);
                yield return new TestCaseData("4d10!>9ro=1rc<2kh2", 4, new DieType.Basic(10), 4);
                yield return new TestCaseData("5d12>10f<2!>11rc=1+7", 5, new DieType.Basic(12), 5);
            }
        }

        [TestCaseSource(nameof(MultipleModifiersTestCases))]
        public void BasicRoll_WithMultipleModifiers_ShouldSucceed(
            string input,
            int expectedNumDice,
            DieType expectedDieType,
            int expectedModifierCount
        )
        {
            var result = DiceParser.basicRoll.Parse(input);
            Assert.IsTrue(result.Success, $"Parser should succeed for input '{input}'");
            
            var roll = result.Value as BasicRoll;
            Assert.NotNull(roll);
            Assert.That(roll.NumberOfDice, Is.EqualTo(expectedNumDice));
            Assert.That(roll.DieType, Is.EqualTo(expectedDieType));
            
            // Count both regular modifiers and constant modifiers (if any)
            int totalModifiers = roll.Modifiers.Count();
            if (input.Contains('+') || input.Contains('-')) {
                Assert.That(totalModifiers, Is.EqualTo(expectedModifierCount), 
                    $"Expected {expectedModifierCount} modifiers but found {totalModifiers}");
            } else {
                Assert.That(totalModifiers, Is.EqualTo(expectedModifierCount), 
                    $"Expected {expectedModifierCount} modifiers but found {totalModifiers}");
            }
        }

        [Test]
        public void BasicRoll_WithMultipleModifiers_ShouldHaveCorrectModifierTypes()
        {
            // Test specific modifier types in a roll with multiple modifiers
            var input = "4d6>3f<2kh2";
            var result = DiceParser.basicRoll.Parse(input);
            Assert.IsTrue(result.Success, $"Parser should succeed for input '{input}'");
            
            var roll = result.Value as BasicRoll;
            Assert.NotNull(roll);
            
            var modifiers = roll.Modifiers.ToList();
            Assert.That(modifiers.Count, Is.EqualTo(3));
            
            Assert.IsTrue(modifiers[0] is SuccessModifier);
            Assert.IsTrue(modifiers[1] is FailureModifier);
            Assert.IsTrue(modifiers[2] is KeepModifier);
            
            var successMod = modifiers[0] as SuccessModifier;
            var failureMod = modifiers[1] as FailureModifier;
            var keepMod = modifiers[2] as KeepModifier;
            
            Assert.That(successMod?.Operator, Is.EqualTo(ComparisonOperator.GreaterThan));
            Assert.That(successMod?.Value, Is.EqualTo(3));
            
            Assert.That(failureMod?.Operator, Is.EqualTo(ComparisonOperator.LessThan));
            Assert.That(failureMod?.Value, Is.EqualTo(2));
            
            Assert.That(keepMod?.Count, Is.EqualTo(2));
            Assert.That(keepMod?.KeepHighest, Is.True);
        }

        [Test]
        public void BasicRoll_WithModifiersAndConstant_ShouldHaveCorrectConstantModifier()
        {
            var input = "3d8>4kh2+5";
            var result = DiceParser.basicRoll.Parse(input);
            Assert.IsTrue(result.Success);
            
            var roll = result.Value as BasicRoll;
            Assert.NotNull(roll);
            
            var modifiers = roll.Modifiers.ToList();
            Assert.That(modifiers.Count, Is.EqualTo(3));
            
            Assert.IsTrue(modifiers[0] is SuccessModifier);
            Assert.IsTrue(modifiers[1] is KeepModifier);
            Assert.IsTrue(modifiers[2] is ConstantModifier);
            
            var constMod = modifiers[2] as ConstantModifier;
            Assert.That(constMod?.Operator, Is.EqualTo(ArithmaticOperator.Add));
            Assert.That(constMod?.Value, Is.EqualTo(5));
        }

        [Test]
        public void BasicRoll_WithComplexModifierCombination_ShouldParseCorrectly()
        {
            var input = "6d10>8f<2!>9ro=1kh3-7";
            var result = DiceParser.basicRoll.Parse(input);
            Assert.IsTrue(result.Success, $"Parser should succeed for input '{input}'");
            
            var roll = result.Value as BasicRoll;
            Assert.NotNull(roll);
            
            var modifiers = roll.Modifiers.ToList();
            Assert.That(modifiers.Count, Is.EqualTo(6));
            
            Assert.IsTrue(modifiers[0] is SuccessModifier);
            Assert.IsTrue(modifiers[1] is FailureModifier);
            Assert.IsTrue(modifiers[2] is ExplodeModifier);
            Assert.IsTrue(modifiers[3] is RerollOnceModifier);
            Assert.IsTrue(modifiers[4] is KeepModifier);
            Assert.IsTrue(modifiers[5] is ConstantModifier);
            
            var constMod = modifiers[5] as ConstantModifier;
            Assert.That(constMod?.Operator, Is.EqualTo(ArithmaticOperator.Subtract));
            Assert.That(constMod?.Value, Is.EqualTo(7));
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
        public void BasicRoll_Parse_InvalidInput_ShouldFail(string input)
        {
            var result = DiceParser.basicRoll.Parse(input);
            Assert.IsFalse(result.Success, $"Basic roll parser should fail for input '{input}'");
        }

        #endregion

        #region White space Tests

        [TestCase(" 3d6 ", 3, 6)]
        [TestCase("3d6", 3, 6)]
        [TestCase("2d10 +5", 2, 10)]
        [TestCase(" 4d8 kh3 ", 4, 8)]
        [TestCase("5d20 >15", 5, 20)]
        [TestCase("3d6 !>3 ro=1", 3, 6)]
        [TestCase(" 2d10 kh2 +3 ", 2, 10)]
        public void BasicRoll_WithWhitespace_ShouldParseCorrectly(string input, int expectedNumDice, int expectedDieSize)
        {
            var result = DiceParser.basicRoll.Parse(input);
            Assert.IsTrue(result.Success, $"Parser should succeed for input with whitespace: '{input}'");
            
            var roll = result.Value as BasicRoll;
            Assert.NotNull(roll);
            Assert.That(roll.NumberOfDice, Is.EqualTo(expectedNumDice));
            
            // Check that the die type is correctly parsed
            DieType.Basic? basicDie = roll.DieType as DieType.Basic;
            Assert.NotNull(basicDie, "Die type should be Basic");
            Assert.That(basicDie.sides, Is.EqualTo(expectedDieSize));
        }

        [Test]
        public void BasicRoll_WithWhitespaceInModifiers_ShouldParseModifiersCorrectly()
        {
            var input = "4d6 >3 f<2 kh2";
            var result = DiceParser.basicRoll.Parse(input);
            Assert.IsTrue(result.Success);
            
            var roll = result.Value as BasicRoll;
            Assert.NotNull(roll);
            
            var modifiers = roll.Modifiers.ToList();
            Assert.That(modifiers.Count, Is.EqualTo(3));
            
            Assert.IsTrue(modifiers[0] is SuccessModifier);
            Assert.IsTrue(modifiers[1] is FailureModifier);
            Assert.IsTrue(modifiers[2] is KeepModifier);
        }

        [Test]
        public void BasicRoll_WithWhitespaceBetweenModifierOperators_ShouldParseCorrectly()
        {
            var input = "3d10 >5 f<2";
            var result = DiceParser.basicRoll.Parse(input);
            Assert.IsTrue(result.Success);
            
            var roll = result.Value as BasicRoll;
            Assert.NotNull(roll);
            
            var modifiers = roll.Modifiers.ToList();
            Assert.That(modifiers.Count, Is.EqualTo(2));
            
            var successMod = modifiers[0] as SuccessModifier;
            Assert.NotNull(successMod);
            Assert.That(successMod.Operator, Is.EqualTo(ComparisonOperator.GreaterThan));
            Assert.That(successMod.Value, Is.EqualTo(5));
            
            var failureMod = modifiers[1] as FailureModifier;
            Assert.NotNull(failureMod);
            Assert.That(failureMod.Operator, Is.EqualTo(ComparisonOperator.LessThan));
            Assert.That(failureMod.Value, Is.EqualTo(2));
        }

        [Test]
        public void ConstantModifier_WithWhitespace_ShouldParseCorrectly()
        {
            var input = "2d6 +5";
            var result = DiceParser.basicRoll.Parse(input);
            Assert.IsTrue(result.Success);
            
            var roll = result.Value as BasicRoll;
            Assert.NotNull(roll);
            
            var modifiers = roll.Modifiers.ToList();
            Assert.That(modifiers.Count, Is.EqualTo(1));
            
            var constMod = modifiers[0] as ConstantModifier;
            Assert.NotNull(constMod);
            Assert.That(constMod.Operator, Is.EqualTo(ArithmaticOperator.Add));
            Assert.That(constMod.Value, Is.EqualTo(5));
        }

        [Test]
        public void ComplexRoll_WithRandomWhitespace_ShouldParseCorrectly()
        {
            var input = " 6d10 >8 f<2 !>9 ro=1 kh3 -7 ";
            var result = DiceParser.basicRoll.Parse(input);
            Assert.IsTrue(result.Success);
            
            var roll = result.Value as BasicRoll;
            Assert.NotNull(roll);
            Assert.That(roll.NumberOfDice, Is.EqualTo(6));
            
            var basicDie = roll.DieType as DieType.Basic;
            Assert.NotNull(basicDie);
            Assert.That(basicDie.sides, Is.EqualTo(10));
            
            var modifiers = roll.Modifiers.ToList();
            Assert.That(modifiers.Count, Is.EqualTo(6));
            
            // Check that all modifiers are of the expected types in the correct order
            Assert.IsTrue(modifiers[0] is SuccessModifier);
            Assert.IsTrue(modifiers[1] is FailureModifier);
            Assert.IsTrue(modifiers[2] is ExplodeModifier);
            Assert.IsTrue(modifiers[3] is RerollOnceModifier);
            Assert.IsTrue(modifiers[4] is KeepModifier);
            Assert.IsTrue(modifiers[5] is ConstantModifier);
        }

        [Test]
        public void SpecialDieTypes_WithWhitespace_ShouldParseCorrectly()
        {
            // Test Fudge dice with whitespace
            var fudgeInput = " 2dF ";
            var fudgeResult = DiceParser.basicRoll.Parse(fudgeInput);
            Assert.IsTrue(fudgeResult.Success, "Fudge dice with whitespace should parse correctly");
            var fudgeRoll = fudgeResult.Value as BasicRoll;
            Assert.NotNull(fudgeRoll);
            Assert.That(fudgeRoll.DieType, Is.TypeOf<DieType.Fudge>());
            
            // Test Percent dice with whitespace
            var percentInput = " 3d% ";
            var percentResult = DiceParser.basicRoll.Parse(percentInput);
            Assert.IsTrue(percentResult.Success, "Percent dice with whitespace should parse correctly");
            var percentRoll = percentResult.Value as BasicRoll;
            Assert.NotNull(percentRoll);
            Assert.That(percentRoll.DieType, Is.TypeOf<DieType.Percent>());
        }

        #endregion

        #region Roll Parser Tests

        [TestCase("d6", typeof(BasicRoll))]
        [TestCase("2d10", typeof(BasicRoll))]
        [TestCase("3d6+5", typeof(BasicRoll))]
        [TestCase("4d8kh3", typeof(BasicRoll))]
        [TestCase("5", typeof(Constant))]
        [TestCase("42", typeof(Constant))]
        public void Roll_Parse_ShouldReturnCorrectType(string input, Type expectedType)
        {
            var result = DiceParser.Roll.Parse(input);
            Assert.IsTrue(result.Success, $"Roll parser should succeed for input '{input}'");
            Assert.That(result.Value, Is.TypeOf(expectedType));
        }

        [TestCase(" d6 ")]
        [TestCase(" 2d10 ")]
        [TestCase(" 3d6+5 ")]
        [TestCase("  4d8kh3  ")]
        [TestCase(" 5 ")]
        [TestCase("  42  ")]
        public void Roll_Parse_ShouldHandleLeadingAndTrailingWhitespace(string input)
        {
            var result = DiceParser.Roll.Parse(input);
            Assert.IsTrue(result.Success, $"Roll parser should succeed for input with whitespace '{input}'");
        }

        [TestCase("2d6 kh3")]
        [TestCase("3d10 +5")]
        [TestCase("2d8 !>6 kh2")]
        public void Roll_Parse_ShouldHandleInternalWhitespace(string input)
        {
            var result = DiceParser.Roll.Parse(input);
            Assert.IsTrue(result.Success, $"Roll parser should succeed for input with internal whitespace '{input}'");
        }

        [Test]
        public void Roll_Parse_BasicRoll_ShouldReturnCorrectProperties()
        {
            var input = "3d8>5kh2";
            var result = DiceParser.Roll.Parse(input);
            Assert.IsTrue(result.Success);
            
            var roll = result.Value as BasicRoll;
            Assert.NotNull(roll);
            Assert.That(roll.NumberOfDice, Is.EqualTo(3));
            
            var dieType = roll.DieType as DieType.Basic;
            Assert.NotNull(dieType);
            Assert.That(dieType.sides, Is.EqualTo(8));
            
            var modifiers = roll.Modifiers.ToList();
            Assert.That(modifiers.Count, Is.EqualTo(2));
            Assert.IsTrue(modifiers[0] is SuccessModifier);
            Assert.IsTrue(modifiers[1] is KeepModifier);
        }

        [Test]
        public void Roll_Parse_Constant_ShouldReturnCorrectValue()
        {
            var input = "42";
            var result = DiceParser.Roll.Parse(input);
            Assert.IsTrue(result.Success);
            
            var constant = result.Value as Constant;
            Assert.NotNull(constant);
            Assert.That(constant.Value, Is.EqualTo(42));
        }

        [TestCase("5a")]
        [TestCase(" 35a ")]
        public void Roll_Parse_Constant_ShouldFail(string input)
        {
            var result = DiceParser.Roll.Parse(input);
            Assert.IsFalse(result.Success, $"Roll parser should fail for invalid constant input '{input}'");
        }

        [TestCase("")]
        [TestCase("d")]
        [TestCase("3+4")]
        [TestCase("dice")]
        [TestCase("3d")]
        [TestCase("3d6extra")]
        [TestCase("3d6abc")]
        public void Roll_Parse_InvalidInput_ShouldFail(string input)
        {
            var result = DiceParser.Roll.Parse(input);
            Assert.IsFalse(result.Success, $"Roll parser should fail for invalid input '{input}'");
        }

        [Test]
        public void Roll_Parse_ComplexRoll_ShouldParseCorrectly()
        {
            var input = "5d10!>8f<2kh3+7";
            var result = DiceParser.Roll.Parse(input);
            Assert.IsTrue(result.Success);
            
            var roll = result.Value as BasicRoll;
            Assert.NotNull(roll);
            Assert.That(roll.NumberOfDice, Is.EqualTo(5));
            
            var dieType = roll.DieType as DieType.Basic;
            Assert.NotNull(dieType);
            Assert.That(dieType.sides, Is.EqualTo(10));
            
            var modifiers = roll.Modifiers.ToList();
            Assert.That(modifiers.Count, Is.EqualTo(4));
            
            Assert.IsTrue(modifiers[0] is ExplodeModifier);
            Assert.IsTrue(modifiers[1] is FailureModifier);
            Assert.IsTrue(modifiers[2] is KeepModifier);
            Assert.IsTrue(modifiers[3] is ConstantModifier);
            
            var explodeMod = modifiers[0] as ExplodeModifier;
            Assert.That(explodeMod?.Operator, Is.EqualTo(ComparisonOperator.GreaterThan));
            Assert.That(explodeMod?.Value, Is.EqualTo(8));
            
            var failureMod = modifiers[1] as FailureModifier;
            Assert.That(failureMod?.Operator, Is.EqualTo(ComparisonOperator.LessThan));
            Assert.That(failureMod?.Value, Is.EqualTo(2));
            
            var keepMod = modifiers[2] as KeepModifier;
            Assert.That(keepMod?.Count, Is.EqualTo(3));
            Assert.That(keepMod?.KeepHighest, Is.True);
            
            var constMod = modifiers[3] as ConstantModifier;
            Assert.That(constMod?.Operator, Is.EqualTo(ArithmaticOperator.Add));
            Assert.That(constMod?.Value, Is.EqualTo(7));
        }

        [Test]
        public void Roll_Parse_SpecialDieTypes_ShouldParseCorrectly()
        {
            // Test Fudge dice
            var fudgeInput = "4dF";
            var fudgeResult = DiceParser.Roll.Parse(fudgeInput);
            Assert.IsTrue(fudgeResult.Success);
            var fudgeRoll = fudgeResult.Value as BasicRoll;
            Assert.NotNull(fudgeRoll);
            Assert.That(fudgeRoll.NumberOfDice, Is.EqualTo(4));
            Assert.That(fudgeRoll.DieType, Is.TypeOf<DieType.Fudge>());
            
            // Test Percent dice
            var percentInput = "2d%";
            var percentResult = DiceParser.Roll.Parse(percentInput);
            Assert.IsTrue(percentResult.Success);
            var percentRoll = percentResult.Value as BasicRoll;
            Assert.NotNull(percentRoll);
            Assert.That(percentRoll.NumberOfDice, Is.EqualTo(2));
            Assert.That(percentRoll.DieType, Is.TypeOf<DieType.Percent>());
        }

        #endregion
    }
}