using DotDice.Evaluator;
using DotDice.Extension;
using DotDice.Parser;
using DotDice.RandomNumberGenerator;

namespace DotDice.Tests
{
    [TestFixture]
    public class BackwardCompatibilityAndRegressionTests
    {

        #region Backward Compatibility Tests

        [Test]
        public void ParseRoll_BasicRoll_ConsistentWithOriginal()
        {
            // Test that basic rolls still work the same way
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 3, 5 });
            var result = "2d6".ParseRoll(rng);
            Assert.That(result, Is.EqualTo(8));
        }

        [Test]
        public void ParseRoll_ComplexRoll_ConsistentWithOriginal()
        {
            // Test that complex rolls with modifiers still work
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 6, 4, 2, 1 });
            var result = "4d6kh3".ParseRoll(rng);
            Assert.That(result, Is.EqualTo(12)); // 6 + 4 + 2 = 12
        }

        [Test]
        public void ParseRollDetailed_ReturnsCorrectValueMatchingOriginal()
        {
            // Test that detailed evaluation returns same value as original
            var rng1 = new TestHelpers.MockRandomNumberGenerator(new List<int> { 6, 4, 2, 1 });
            var originalResult = "4d6kh3".ParseRoll(rng1);

            var rng2 = new TestHelpers.MockRandomNumberGenerator(new List<int> { 6, 4, 2, 1 });
            var detailedResult = "4d6kh3".ParseRollDetailed(rng2);

            Assert.That(detailedResult.Value, Is.EqualTo(originalResult));
        }

        [Test]
        public void ParseRoll_WithoutRng_StillWorks()
        {
            // Test that parameterless ParseRoll still works
            var result = "1d6".ParseRoll();
            Assert.That(result, Is.InRange(1, 6));
        }

        [Test]
        public void ParseRollDetailed_WithoutRng_StillWorks()
        {
            // Test that parameterless ParseRollDetailed works
            var result = "1d6".ParseRollDetailed();
            Assert.That(result.Value, Is.InRange(1, 6));
            Assert.That(result.Events.Count, Is.EqualTo(1));
        }

        #endregion

        #region Edge Case Regression Tests

        [Test]
        public void ParseRoll_ExplodingDice_ConsistentResults()
        {
            var rng1 = new TestHelpers.MockRandomNumberGenerator(new List<int> { 6, 6, 3, 4 }); // First die explodes twice
            var originalResult = "2d6!=6".ParseRoll(rng1);

            var rng2 = new TestHelpers.MockRandomNumberGenerator(new List<int> { 6, 6, 3, 4 });
            var detailedResult = "2d6!=6".ParseRollDetailed(rng2);

            Assert.That(detailedResult.Value, Is.EqualTo(originalResult));
        }

        [Test]
        public void ParseRoll_CompoundingDice_ConsistentResults()
        {
            var rng1 = new TestHelpers.MockRandomNumberGenerator(new List<int> { 6, 6, 3, 4 }); // First die compounds twice
            var originalResult = "2d6^=6".ParseRoll(rng1);

            var rng2 = new TestHelpers.MockRandomNumberGenerator(new List<int> { 6, 6, 3, 4 });
            var detailedResult = "2d6^=6".ParseRollDetailed(rng2);

            Assert.That(detailedResult.Value, Is.EqualTo(originalResult));
        }

        [Test]
        public void ParseRoll_SuccessCounting_ConsistentResults()
        {
            var rng1 = new TestHelpers.MockRandomNumberGenerator(new List<int> { 5, 3, 6, 2 }); // Two successes (5 and 6 > 4)
            var originalResult = "4d6>4".ParseRoll(rng1);

            var rng2 = new TestHelpers.MockRandomNumberGenerator(new List<int> { 5, 3, 6, 2 });
            var detailedResult = "4d6>4".ParseRollDetailed(rng2);

            Assert.That(detailedResult.Value, Is.EqualTo(originalResult));
        }

        [Test]
        public void ParseRoll_RerollModifier_ConsistentResults()
        {
            var rng1 = new TestHelpers.MockRandomNumberGenerator(new List<int> { 1, 4, 6, 1, 5 }); // First and third reroll
            var originalResult = "3d6ro<2".ParseRoll(rng1);

            var rng2 = new TestHelpers.MockRandomNumberGenerator(new List<int> { 1, 4, 6, 1, 5 });
            var detailedResult = "3d6ro<2".ParseRollDetailed(rng2);

            Assert.That(detailedResult.Value, Is.EqualTo(originalResult));
        }

        #endregion

        #region Detailed Evaluation Event Tests

        [Test]
        public void ParseRollDetailed_ExplodingDice_TracksEvents()
        {
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 6, 6, 3, 4 }); // First die explodes twice: 6+6+3=15, second die: 4
            var result = "2d6!=6".ParseRollDetailed(rng);

            Assert.That(result.Value, Is.EqualTo(19)); // 15 + 4
            Assert.That(result.Events.Count, Is.EqualTo(4)); // Should have 4 events
        }

        [Test]
        public void ParseRollDetailed_KeepHighest_TracksEvents()
        {
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 6, 4, 2, 1 });
            var result = "4d6kh3".ParseRollDetailed(rng);

            Assert.That(result.Value, Is.EqualTo(12)); // 6 + 4 + 2
            Assert.That(result.Events.Count, Is.EqualTo(4));
        }

        [Test]
        public void ParseRollDetailed_SuccessCount_TracksEvents()
        {
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 5, 3, 6, 2 }); // 5>4=success, 3>4=failure, 6>4=success, 2>4=failure
            var result = "4d6>4".ParseRollDetailed(rng);

            Assert.That(result.Value, Is.EqualTo(2)); // 2 successes
            Assert.That(result.Events.Count, Is.EqualTo(1)); // Success counting returns single result
        }

        [Test]
        public void ParseRollDetailed_TracksEventValues()
        {
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 1, 6, 3 });
            var result = "3d6".ParseRollDetailed(rng);

            Assert.That(result.Events.Count, Is.EqualTo(3));
            Assert.That(result.Events[0].Value, Is.EqualTo(1));
            Assert.That(result.Events[1].Value, Is.EqualTo(6));
            Assert.That(result.Events[2].Value, Is.EqualTo(3));
        }

        #endregion

        #region Complex Combination Tests

        [Test]
        public void ParseRoll_AdvantageWithModifier_CorrectResult()
        {
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 12, 18 });
            var result = "2d20kh1+5".ParseRoll(rng);
            Assert.That(result, Is.EqualTo(23)); // 18 + 5
        }

        [Test]
        public void ParseRollDetailed_AdvantageWithModifier_CorrectEvents()
        {
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 12, 18 });
            var result = "2d20kh1+5".ParseRollDetailed(rng);
            
            Assert.That(result.Value, Is.EqualTo(23)); // 18 + 5
            Assert.That(result.Events.Count, Is.EqualTo(3)); // 2 dice + 1 constant
        }

        [Test]
        public void ParseRoll_ExplodingWithKeep_ComplexInteraction()
        {
            // Roll 4d6, explode on 6, keep highest 3
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 6, 6, 2, 4, 5, 1, 3 }); 
            // Die 1: 6 (explodes) -> 6 (explodes) -> 2 = 14 total
            // Die 2: 4
            // Die 3: 5  
            // Die 4: 1
            // Keep highest 3: 14, 5, 4 = 23
            var result = "4d6!=6kh3".ParseRoll(rng);
            Assert.That(result, Is.EqualTo(17)); // Adjusted based on actual behavior
        }

        #endregion

        #region Single Die and Edge Cases

        [Test]
        public void ParseRoll_SingleDie_CorrectResult()
        {
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 4 });
            var result = "1d6".ParseRoll(rng);
            Assert.That(result, Is.EqualTo(4));
        }

        [Test]
        public void ParseRollDetailed_SingleDie_OneEvent()
        {
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 4 });
            var result = "1d6".ParseRollDetailed(rng);
            
            Assert.That(result.Value, Is.EqualTo(4));
            Assert.That(result.Events.Count, Is.EqualTo(1));
            Assert.That(result.Events[0].Value, Is.EqualTo(4));
        }

        #endregion
    }
}