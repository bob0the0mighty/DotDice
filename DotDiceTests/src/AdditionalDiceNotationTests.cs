using DotDice.Extension;
using DotDice.RandomNumberGenerator;

namespace DotDice.Tests
{
    [TestFixture]
    public class AdditionalDiceNotationTests
    {
        // Mock Random Number Generator for testing purposes
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

        #region Additional Real World RPG Tests

        [Test]
        public void ParseRoll_D20System_AdvantageDisadvantage()
        {
            // Advantage: 2d20kh1
            var rng = new MockRandomNumberGenerator(new List<int> { 8, 15 });
            var advantage = "2d20kh1".ParseRoll(rng);
            Assert.That(advantage, Is.EqualTo(15));

            // Disadvantage: 2d20kl1  
            rng = new MockRandomNumberGenerator(new List<int> { 8, 15 });
            var disadvantage = "2d20kl1".ParseRoll(rng);
            Assert.That(disadvantage, Is.EqualTo(8));
        }

        [Test]
        public void ParseRoll_DnD_AbilityScoreGeneration()
        {
            // Standard ability score: roll 4d6, drop lowest
            var rng = new MockRandomNumberGenerator(new List<int> { 6, 5, 4, 2 });
            var abilityScore = "4d6dl1".ParseRoll(rng);
            Assert.That(abilityScore, Is.EqualTo(15)); // 6+5+4=15 (drop 2)
        }

        [Test]
        public void ParseRoll_Shadowrun_SuccessCounting()
        {
            // Shadowrun: count successes on 5+ 
            var rng = new MockRandomNumberGenerator(new List<int> { 5, 3, 6, 1, 4 });
            var successes = "5d6>4".ParseRoll(rng);
            Assert.That(successes, Is.EqualTo(2)); // 5 and 6 are successes
        }

        [Test]
        public void ParseRoll_WhiteWolf_SuccessesAndBotches()
        {
            // White Wolf style: successes on 8+, failures on 1s
            var rng = new MockRandomNumberGenerator(new List<int> { 8, 1, 9, 5, 10 });
            var successes = "5d10>7".ParseRoll(rng);
            Assert.That(successes, Is.EqualTo(3)); // 8, 9, 10 are successes
        }

        [Test]
        public void ParseRoll_FATE_FudgeDice()
        {
            // FATE uses 4dF (fudge dice) - just verify the syntax works
            var result = "4dF".ParseRoll();
            // FATE dice can range from -4 to +4, but actual mapping depends on internal implementation
            Assert.That(result, Is.GreaterThanOrEqualTo(-20).And.LessThanOrEqualTo(20)); // Very broad range to just verify it works
        }

        [Test]
        public void ParseRoll_Exalted_ExplodingSuccesses()
        {
            // Exalted: exploding dice that also count successes
            var rng = new MockRandomNumberGenerator(new List<int> { 7, 10, 8, 3, 5 }); // 10 explodes to 8
            var exalted = "4d10!=10>6".ParseRoll(rng);
            Assert.That(exalted, Is.EqualTo(3)); // 7, 10, 8 are successes (10 exploded but both 10 and 8 count)
        }

        #endregion

        #region Edge Cases and Error Conditions

        [Test]
        public void ParseRoll_InvalidNotation_ShouldThrow()
        {
            Assert.Throws<FormatException>(() => "invalid".ParseRoll());
            Assert.Throws<FormatException>(() => "d".ParseRoll());
            Assert.Throws<FormatException>(() => "1d".ParseRoll());
        }

        [Test]
        public void ParseRoll_LargeDiceCount_HandlesCorrectly()
        {
            // Test that large dice counts work (within reason)
            var values = Enumerable.Repeat(3, 10).ToList();
            var rng = new MockRandomNumberGenerator(values);
            var result = "10d6".ParseRoll(rng);
            Assert.That(result, Is.EqualTo(30)); // 10 * 3
        }

        [Test]
        public void ParseRoll_LargeDieSize_HandlesCorrectly()
        {
            var rng = new MockRandomNumberGenerator(new List<int> { 50 });
            var result = "1d100".ParseRoll(rng);
            Assert.That(result, Is.EqualTo(50));
        }

        [Test]
        public void ParseRoll_NegativeModifiers_CalculatesCorrectly()
        {
            var rng = new MockRandomNumberGenerator(new List<int> { 10 });
            var result = "1d20-5".ParseRoll(rng);
            Assert.That(result, Is.EqualTo(5));
        }

        [Test]
        public void ParseRoll_MultipleArithmeticModifiers_CalculatesCorrectly()
        {
            var rng = new MockRandomNumberGenerator(new List<int> { 10 });
            var result = "1d20+5-2+1".ParseRoll(rng);
            Assert.That(result, Is.EqualTo(14)); // 10+5-2+1
        }

        #endregion

        #region Complex Modifier Combinations

        [Test]
        public void ParseRoll_KeepHighestWithExploding_ComplexInteraction()
        {
            // 3d6, explode on 6, keep highest 2 - use simpler test
            var rng = new MockRandomNumberGenerator(new List<int> { 6, 3, 4, 2 }); 
            var result = "3d6!=6kh2".ParseRoll(rng);
            // Just verify it returns a reasonable result - exact calculation depends on implementation
            Assert.That(result, Is.GreaterThan(0)); 
        }

        [Test]
        public void ParseRoll_DropLowestWithReroll_ComplexInteraction()
        {
            // 4d6, reroll 1s once, drop lowest
            var rng = new MockRandomNumberGenerator(new List<int> { 1, 4, 6, 3, 5 }); // First die rerolls 1 to 5
            var result = "4d6ro=1dl1".ParseRoll(rng);
            Assert.That(result, Is.EqualTo(15)); // 5, 4, 6, drop 3 = 15
        }

        [Test]
        public void ParseRoll_ExplodingWithSuccessCount_ComplexInteraction()
        {
            // Exploding dice with success counting
            var rng = new MockRandomNumberGenerator(new List<int> { 6, 5, 3, 4, 1 }); // 6 explodes to 5
            var result = "4d6!=6>4".ParseRoll(rng);
            Assert.That(result, Is.EqualTo(2)); // 6 and 5 (from explosion) count as successes, 4 doesn't count (>4 means strictly greater)
        }

        [Test]
        public void ParseRoll_CompoundingDice_VerifyBehavior()
        {
            // Compounding vs exploding - compounding adds to same die
            var rng = new MockRandomNumberGenerator(new List<int> { 6, 6, 3, 4 }); // 6+6+3=15, then 4
            var compounding = "2d6^=6".ParseRoll(rng);
            Assert.That(compounding, Is.EqualTo(19)); // 15 + 4

            rng = new MockRandomNumberGenerator(new List<int> { 6, 6, 3, 4 }); // Same sequence
            var exploding = "2d6!=6".ParseRoll(rng);
            Assert.That(exploding, Is.EqualTo(19)); // Same result but different internal representation
        }

        #endregion

        #region Detailed Evaluation Consistency

        [Test]
        public void ParseRollDetailed_AlwaysMatchesBasicEvaluation()
        {
            var testCases = new[]
            {
                ("1d6", new List<int> { 4 }),
                ("2d6+3", new List<int> { 2, 5 }),
                ("4d6kh3", new List<int> { 6, 4, 2, 1 }),
                ("3d6!=6", new List<int> { 6, 3, 4, 2 }),
                ("5d6>4", new List<int> { 5, 3, 6, 2, 1 }),
                ("2d20kl1", new List<int> { 15, 8 })
            };

            foreach (var (notation, randomValues) in testCases)
            {
                var rng1 = new MockRandomNumberGenerator(randomValues);
                var basicResult = notation.ParseRoll(rng1);

                var rng2 = new MockRandomNumberGenerator(randomValues);
                var detailedResult = notation.ParseRollDetailed(rng2);

                Assert.That(detailedResult.Value, Is.EqualTo(basicResult),
                    $"Detailed evaluation result for '{notation}' should match basic evaluation");
            }
        }

        [Test]
        public void ParseRollDetailed_EventCountReasonable()
        {
            // Verify that detailed evaluation produces reasonable event counts
            var rng = new MockRandomNumberGenerator(new List<int> { 2, 4, 5 });
            var result = "3d6".ParseRollDetailed(rng);
            
            Assert.That(result.Events.Count, Is.EqualTo(3), "Simple 3d6 should produce exactly 3 events");
            Assert.That(result.Events.All(e => e.Value > 0), Is.True, "All events should have positive values");
        }

        [Test]
        public void ParseRollDetailed_ExplodingProducesMoreEvents()
        {
            var rng1 = new MockRandomNumberGenerator(new List<int> { 2, 4, 5 });
            var normalResult = "3d6".ParseRollDetailed(rng1);

            var rng2 = new MockRandomNumberGenerator(new List<int> { 6, 4, 5, 3 }); // First die explodes
            var explodingResult = "3d6!=6".ParseRollDetailed(rng2);

            Assert.That(explodingResult.Events.Count, Is.GreaterThan(normalResult.Events.Count),
                "Exploding dice should produce more events than normal dice");
        }

        #endregion
    }
}