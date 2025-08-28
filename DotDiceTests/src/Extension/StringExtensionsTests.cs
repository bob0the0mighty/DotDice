using DotDice.Extension;
using static DotDice.Tests.DiceEvaluatorTests;

namespace DotDice.Tests
{
    [TestFixture]
    public class StringExtensionsTests
    {
        #region Basic Roll Tests

        [Theory]
        [TestCaseSource(nameof(ParseRoll_BasicRolls_TestCases))]
        public void ParseRoll_BasicRolls_ReturnsExpectedResult(string input, List<int> randomValues, int expectedResult)
        {
            var mockRng = new MockRandomNumberGenerator(randomValues);
            int result = input.ParseRoll(mockRng);
            Assert.AreEqual(expectedResult, result, $"Roll result should be {expectedResult}");
        }

        public static IEnumerable<TestCaseData> ParseRoll_BasicRolls_TestCases()
        {
            yield return new TestCaseData("d6", new List<int> { 4 }, 4);
            yield return new TestCaseData("1d6", new List<int> { 6 }, 6);
            yield return new TestCaseData("2d6", new List<int> { 5, 3 }, 8);
            yield return new TestCaseData("1d20", new List<int> { 17 }, 17);
            yield return new TestCaseData("3d10", new List<int> { 7, 5, 9 }, 21);
            yield return new TestCaseData("10d4", new List<int> { 3, 2, 4, 1, 4, 3, 2, 4, 1, 2 }, 26);
            yield return new TestCaseData("1d100", new List<int> { 75 }, 75);
            yield return new TestCaseData("d%", new List<int> { 42 }, 42);
            yield return new TestCaseData("2dF", new List<int> { 1, -1 }, 0);
        }

        #endregion

        #region Modifier Tests

        [Theory]
        [TestCaseSource(nameof(ParseRoll_ModifiedRolls_TestCases))]
        public void ParseRoll_ModifiedRolls_ReturnsExpectedResult(string input, List<int> randomValues, int expectedResult)
        {
            var mockRng = new MockRandomNumberGenerator(randomValues);
            int result = input.ParseRoll(mockRng);
            Assert.AreEqual(expectedResult, result, $"Roll result should be {expectedResult}");
        }

        public static IEnumerable<TestCaseData> ParseRoll_ModifiedRolls_TestCases()
        {
            // Keep/Drop modifiers
            yield return new TestCaseData("4d6kh3", new List<int> { 6, 4, 3, 1 }, 13);
            yield return new TestCaseData("4d6kl3", new List<int> { 6, 4, 3, 1 }, 8);
            yield return new TestCaseData("4d6dh1", new List<int> { 6, 4, 3, 1 }, 8);
            yield return new TestCaseData("4d6dl1", new List<int> { 6, 4, 3, 1 }, 13);
            
            // Arithmetic modifiers
            yield return new TestCaseData("1d20+5", new List<int> { 15 }, 20);
            yield return new TestCaseData("2d6-2", new List<int> { 5, 3 }, 6);
            yield return new TestCaseData("3d8+10", new List<int> { 7, 5, 3 }, 25);
            
            // Explode modifiers
            yield return new TestCaseData("1d6!=6", new List<int> { 6, 4 }, 10);
            yield return new TestCaseData("1d6!>5", new List<int> { 6, 4 }, 10);
            
            // Compound modifiers
            yield return new TestCaseData("1d6^=6", new List<int> { 6, 5, 4 }, 11);
            yield return new TestCaseData("1d6^>4", new List<int> { 6, 5, 4 }, 15);
            
            // Reroll modifiers
            yield return new TestCaseData("1d6ro<3", new List<int> { 2, 5 }, 5);
            yield return new TestCaseData("1d6rc<3", new List<int> { 2, 1, 4 }, 4);
            
            // Success/Failure modifiers
            yield return new TestCaseData("10d6>4", new List<int> { 5, 3, 6, 2, 4, 5, 2, 6, 3, 1 }, 4);
            yield return new TestCaseData("10d6f<3", new List<int> { 5, 3, 6, 2, 4, 5, 2, 6, 3, 1 }, -3);
        }

        #endregion

        #region Complex Notation Tests

        [Theory]
        [TestCaseSource(nameof(ParseRoll_ComplexNotations_TestCases))]
        public void ParseRoll_ComplexNotations_ReturnsExpectedResult(string input, List<int> randomValues, int expectedResult)
        {
            var mockRng = new MockRandomNumberGenerator(randomValues);
            int result = input.ParseRoll(mockRng);
            Assert.AreEqual(expectedResult, result, $"Roll result should be {expectedResult}");
        }

        public static IEnumerable<TestCaseData> ParseRoll_ComplexNotations_TestCases()
        {
            // Combinations of modifiers
            yield return new TestCaseData("4d6kh3+5", new List<int> { 6, 4, 3, 1 }, 18);
            yield return new TestCaseData("2d20kh1+3", new List<int> { 18, 12 }, 21);
            yield return new TestCaseData("3d6!=6kh2", new List<int> { 6, 3, 5, 4 }, 11);
            yield return new TestCaseData("5d10>8", new List<int> { 9, 7, 10, 5, 3 }, 2);
            yield return new TestCaseData("3d10!>8", new List<int> { 9, 7, 10, 10, 5, 3 }, 44);
            yield return new TestCaseData("4d6dl1+2", new List<int> { 5, 3, 6, 2 }, 16);
            yield return new TestCaseData("3d8ro<3", new List<int> { 2, 5, 1, 7, 3 }, 15);
            yield return new TestCaseData("2d6^=6kh1", new List<int> { 6, 3, 6, 4 }, 16);
        }

        #endregion

        #region Error Tests

        [Theory]
        [TestCaseSource(nameof(ParseRoll_InvalidInputs_TestCases))]
        public void ParseRoll_InvalidInputs_ThrowsFormatException(string input)
        {
            Assert.Throws<FormatException>(() => input.ParseRoll());
        }

        public static IEnumerable<TestCaseData> ParseRoll_InvalidInputs_TestCases()
        {
            yield return new TestCaseData("not a roll");
            yield return new TestCaseData("d0");  // Dice with 0 sides
            yield return new TestCaseData("0d6"); // 0 dice
            yield return new TestCaseData("d");   // Missing sides
            yield return new TestCaseData("dlc"); // Invalid drop/keep syntax
            yield return new TestCaseData("d6k"); // Incomplete modifier
            yield return new TestCaseData("d6+"); // Missing value after operator
            yield return new TestCaseData("d6+abc"); // Non-numeric modifier
            yield return new TestCaseData("d6!>"); // Incomplete explode modifier
        }

        #endregion

        #region Whitespace Tests

        [Test]
        public void ParseRoll_WithWhitespaces_ReturnsExpectedResult()
        {
            // Use consistent random values for all tests
            var mockRng = new RepeatingRandomNumberGenerator(4);
            
            int result1 = "1d6".ParseRoll(mockRng);
            int result2 = " 1d6".ParseRoll(mockRng);
            int result3 = "1d6 ".ParseRoll(mockRng);
            int result4 = " 1d6 ".ParseRoll(mockRng);
            
            // All should have the same result
            Assert.AreEqual(4, result1);
            Assert.AreEqual(4, result2);
            Assert.AreEqual(4, result3);
            Assert.AreEqual(4, result4);
        }

        #endregion

        #region Real-world Scenarios

        [Theory]
        [TestCaseSource(nameof(ParseRoll_RealWorldScenarios_TestCases))]
        public void ParseRoll_RealWorldScenarios_ReturnsExpectedResult(string input, List<int> randomValues, int expectedResult, string description)
        {
            var mockRng = new MockRandomNumberGenerator(randomValues);
            int result = input.ParseRoll(mockRng);
            Assert.AreEqual(expectedResult, result, $"{description}: Roll result should be {expectedResult}");
        }

        public static IEnumerable<TestCaseData> ParseRoll_RealWorldScenarios_TestCases()
        {
            // D&D ability score generation
            yield return new TestCaseData("4d6kh3", new List<int> { 6, 4, 5, 3 }, 15, "D&D ability score");
            
            // D&D attack roll with advantage
            yield return new TestCaseData("2d20kh1+5", new List<int> { 18, 12 }, 23, "D&D attack with advantage");
            
            // D&D damage roll with critical hit (double dice)
            yield return new TestCaseData("4d8+3", new List<int> { 7, 5, 6, 4 }, 25, "D&D critical damage");
            
            // Shadowrun success test (count 5+ as successes)
            yield return new TestCaseData("8d6>4", new List<int> { 6, 3, 5, 2, 6, 4, 1, 5 }, 4, "Shadowrun success test");
            
            // World of Darkness roll with exploding 10s
            yield return new TestCaseData("5d10!=10>7", new List<int> { 10, 8, 6, 9, 3, 10, 7 }, 4, "WoD with exploding 10s");
            
            // Fudge/FATE dice
            yield return new TestCaseData("4dF+2", new List<int> { 1, 0, -1, 1 }, 3, "FATE skill check");
        }

        #endregion

        #region Repeating Random Number Generator Tests

        [Test]
        public void ParseRoll_WithRepeatingRandomNumberGenerator_ReturnsExpectedResult()
        {
            var repeatingRng = new RepeatingRandomNumberGenerator(6);
            
            // Test with exploding dice (should hit the explosion limit)
            int result1 = "1d6!=6".ParseRoll(repeatingRng);
            Assert.AreEqual(606, result1, "Exploding dice should hit the explosion limit");
            
            // Test with compound dice (should hit the compound limit)
            int result2 = "1d6^=6".ParseRoll(repeatingRng);
            Assert.AreEqual(606, result2, "Compounding dice should hit the compound limit");
            
            // Test with success counting
            var repeatingRng3 = new RepeatingRandomNumberGenerator(5);
            int result3 = "10d6>4".ParseRoll(repeatingRng3);
            Assert.AreEqual(10, result3, "All dice should succeed");
        }

        #endregion

        #region Arithmetic Roll Tests

        [Theory]
        [TestCaseSource(nameof(ParseRoll_ArithmeticExpressions_TestCases))]
        public void ParseRoll_ArithmeticExpressions_ReturnsExpectedResult(string input, List<int> randomValues, int expectedResult)
        {
            var mockRng = new MockRandomNumberGenerator(randomValues);
            int result = input.ParseRoll(mockRng);
            Assert.AreEqual(expectedResult, result, $"Arithmetic roll result should be {expectedResult}");
        }

        public static IEnumerable<TestCaseData> ParseRoll_ArithmeticExpressions_TestCases()
        {
            // Simple arithmetic with constants
            yield return new TestCaseData("3+4", new List<int> { }, 7);
            yield return new TestCaseData("10-3", new List<int> { }, 7);
            
            // Mix of dice and constants
            yield return new TestCaseData("1d6+2", new List<int> { 4 }, 6);
            yield return new TestCaseData("2d6-1", new List<int> { 3, 4 }, 6);
            yield return new TestCaseData("1d20+5", new List<int> { 15 }, 20);
            
            // Multiple dice rolls
            yield return new TestCaseData("1d6+1d4", new List<int> { 5, 3 }, 8);
            yield return new TestCaseData("2d6+3d4", new List<int> { 4, 6, 2, 3, 1 }, 16);
            yield return new TestCaseData("1d20-1d4", new List<int> { 15, 2 }, 13);
            
            // Complex expressions like the issue example (using ro<2 instead of rl2)
            yield return new TestCaseData("3d20+5d6-1d4ro<2+1", new List<int> { 10, 15, 8, 4, 5, 6, 2, 3, 1, 3 }, 51);
            // 3d20: 10+15+8=33, 5d6: 4+5+6+2+3=20, 1d4ro<2: 1 rerolled to 3, total: 33+20-3+1=51
            
            // Multi-step arithmetic
            yield return new TestCaseData("1d6+2d4-3+1d8", new List<int> { 6, 3, 2, 5 }, 13);
            // 1d6: 6, 2d4: 3+2=5, constant: -3, 1d8: 5, total: 6+5-3+5=13
        }

        #endregion
    }
}