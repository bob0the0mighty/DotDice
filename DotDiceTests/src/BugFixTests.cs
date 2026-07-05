using DotDice.Evaluator;
using DotDice.Extension;
using DotDice.Parser;
using DotDice.RandomNumberGenerator;
using static DotDice.Tests.DiceEvaluatorTests;

namespace DotDice.Tests
{
    /// <summary>
    /// Regression tests for the fixes on the dotDice branch:
    ///  1. Rerolls must roll the original die, not a die whose sides equal the previous result.
    ///  2. Success + failure modifiers combined must count both against the same dice.
    ///  3. MaxRerolls is configurable and validated.
    ///  4. Evaluate() and EvaluateDetailed() share one implementation and always agree.
    ///  5. Grammar shorthands (bare "!", "!!", "^", "ro1", "rc2") evaluate correctly.
    ///  6. Parse failures carry a diagnostic message.
    /// </summary>
    [TestFixture]
    public class BugFixTests
    {
        /// <summary>
        /// RNG that records the (min, max) range of every ranged call, so tests can
        /// assert which die was actually rolled (a scripted mock alone can't catch
        /// a wrong side count because it ignores the requested range).
        /// </summary>
        private class RangeRecordingRandomNumberGenerator : IRandomNumberGenerator<int>
        {
            private readonly Queue<int> _values;
            public List<(int min, int max)> RangedCalls { get; } = new();

            public RangeRecordingRandomNumberGenerator(IEnumerable<int> values)
            {
                _values = new Queue<int>(values);
            }

            public int Next() => _values.Dequeue();

            public int Next(int maxValue) => _values.Dequeue();

            public int Next(int minValue, int maxValue)
            {
                RangedCalls.Add((minValue, maxValue));
                return _values.Dequeue();
            }

            public void SetSeed(int seed) { /* No-op for testing */ }

            public int GetSeed() => throw new NotImplementedException();
        }

        #region Reroll uses the original die's sides

        [Test]
        public void RerollOnce_ParseRoll_RerollsUseOriginalDieSides()
        {
            // 3d6ro=1 with a 1 in the pool: the reroll must request a d6 range (1..7),
            // not a range derived from the previous result (the old bug rolled a "d1").
            var rng = new RangeRecordingRandomNumberGenerator(new List<int> { 1, 4, 6, 5 });
            var result = "3d6ro=1".ParseRoll(rng);

            Assert.That(result, Is.EqualTo(15)); // 5 (reroll) + 4 + 6
            Assert.That(rng.RangedCalls.Count, Is.EqualTo(4), "3 initial rolls + 1 reroll");
            Assert.That(rng.RangedCalls, Is.All.EqualTo((1, 7)),
                "Every roll, including the reroll, must be rolled on the original d6");
        }

        [Test]
        public void RerollMultiple_ParseRoll_RerollsUseOriginalDieSides()
        {
            // 1d10rc<3: 2 -> 1 -> 7. Both rerolls must be on a d10 (1..11).
            var rng = new RangeRecordingRandomNumberGenerator(new List<int> { 2, 1, 7 });
            var result = "1d10rc<3".ParseRoll(rng);

            Assert.That(result, Is.EqualTo(7));
            Assert.That(rng.RangedCalls, Is.All.EqualTo((1, 11)),
                "Every reroll must be rolled on the original d10");
        }

        [Test]
        public void RerollOnce_SeededRealRng_AllEventsWithinDieRange()
        {
            // Property-style check with the real RNG: no event value can ever leave 1..6.
            for (int seed = 0; seed < 50; seed++)
            {
                var result = "10d6ro<3".ParseRollDetailed(new RandomIntGenerator(seed));

                Assert.That(result.Events.Select(e => e.Value), Is.All.InRange(1, 6),
                    $"Seed {seed}: rerolled dice must stay within the d6 range");
                Assert.That(result.Value, Is.InRange(10, 60), $"Seed {seed}: total out of range");
            }
        }

        #endregion

        #region Success + failure combined

        [Test]
        public void SuccessAndFailure_ParseRoll_ReturnsSuccessesMinusFailures()
        {
            // World of Darkness style: 6d10, successes on 9+, botches below 2.
            // Rolls: 9, 1, 10, 5, 1, 8 -> successes {9, 10} = 2, failures {1, 1} = 2, net 0.
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 9, 1, 10, 5, 1, 8 });
            var result = "6d10>8f<2".ParseRoll(rng);

            Assert.That(result, Is.EqualTo(0), "2 successes - 2 failures should be 0");
        }

        [Test]
        public void SuccessAndFailure_ParseRollDetailed_ReturnsSuccessesMinusFailures()
        {
            // Rolls: 8, 1, 9, 3, 10 -> successes {8, 9, 10} = 3, failures {1} = 1, net 2.
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 8, 1, 9, 3, 10 });
            var result = "5d10>7f=1".ParseRollDetailed(rng);

            Assert.That(result.Value, Is.EqualTo(2), "3 successes - 1 failure should be 2");
            Assert.That(result.Events.Count, Is.EqualTo(1), "Success/failure counting returns a single count event");
        }

        [Test]
        public void SuccessAndFailure_OverlappingCriteria_SuccessTakesPrecedence()
        {
            // ">2" and "f>4" overlap for a 6: the 6 counts as a success, not a failure.
            // Rolls: 6, 3, 1 -> successes {6, 3} = 2, failures {} (1 matches neither), net 2.
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 6, 3, 1 });
            var result = "3d6>2f>4".ParseRoll(rng);

            Assert.That(result, Is.EqualTo(2), "A die matching both criteria must only count as a success");
        }

        [Test]
        public void SuccessOnly_And_FailureOnly_BehaviorUnchanged()
        {
            var rng1 = new TestHelpers.MockRandomNumberGenerator(new List<int> { 5, 3, 6, 2 });
            Assert.That("4d6>4".ParseRoll(rng1), Is.EqualTo(2), "Success-only counting unchanged");

            var rng2 = new TestHelpers.MockRandomNumberGenerator(new List<int> { 5, 3, 6, 2 });
            Assert.That("4d6f<3".ParseRoll(rng2), Is.EqualTo(-1), "Failure-only counting unchanged");
        }

        [Test]
        public void SuccessAndFailure_WithKeptAndDroppedDice_OnlyCountsActiveDice()
        {
            // 4d10kh2>7f<3: keep the two highest (9, 8), then count successes/failures
            // among the kept dice only. 9 and 8 are successes -> 2; dropped 2 and 5 don't count.
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 9, 2, 8, 5 });
            var result = "4d10kh2>7f<3".ParseRoll(rng);

            Assert.That(result, Is.EqualTo(2), "Dropped dice must not count toward successes or failures");
        }

        #endregion

        #region MaxRerolls

        [Test]
        public void MaxRerolls_SetBelowOne_Throws()
        {
            var evaluator = new DiceEvaluator();
            Assert.Throws<ArgumentOutOfRangeException>(() => evaluator.MaxRerolls = 0);
        }

        [Test]
        public void MaxRerolls_DefaultLimitsRerollChain()
        {
            // rc<7 on a d6 always matches, so the chain stops at the MaxRerolls default (10):
            // 1 initial event + 10 reroll events.
            var evaluator = new DiceEvaluator(new RepeatingRandomNumberGenerator(1));
            var roll = new BasicRoll(1, new DieType.Basic(6),
                new List<Modifier> { new RerollMultipleModifier(ComparisonOperator.LessThan, 7) });

            var result = evaluator.EvaluateDetailed(roll);

            Assert.That(result.Events.Count, Is.EqualTo(11), "1 initial + 10 rerolls (default MaxRerolls)");
        }

        [Test]
        public void MaxRerolls_CustomValueLimitsRerollChain()
        {
            var evaluator = new DiceEvaluator(new RepeatingRandomNumberGenerator(1));
            evaluator.MaxRerolls = 3;
            var roll = new BasicRoll(1, new DieType.Basic(6),
                new List<Modifier> { new RerollMultipleModifier(ComparisonOperator.LessThan, 7) });

            var result = evaluator.EvaluateDetailed(roll);

            Assert.That(result.Events.Count, Is.EqualTo(4), "1 initial + 3 rerolls (custom MaxRerolls)");
        }

        #endregion

        #region Evaluate and EvaluateDetailed agree

        // A corpus of expressions covering every modifier; with the same seed both
        // APIs must produce the same value, guarding against the two paths diverging again.
        [TestCase("3d6")]
        [TestCase("4d6kh3")]
        [TestCase("4d6dl1")]
        [TestCase("2d20kl1")]
        [TestCase("3d6!=6")]
        [TestCase("2d6^=6")]
        [TestCase("4d6ro=1")]
        [TestCase("4d6rc<3")]
        [TestCase("8d6>4")]
        [TestCase("8d6f<2")]
        [TestCase("6d10>8f<2")]
        [TestCase("4d6kh3+5")]
        [TestCase("3d20+5d6-1d4+1")]
        [TestCase("2d6!")]
        [TestCase("2d6!!")]
        [TestCase("4d6ro1dl1")]
        [TestCase("d%")]
        [TestCase("4dF+2")]
        public void Evaluate_And_EvaluateDetailed_AgreeForSameSeed(string expression)
        {
            for (int seed = 0; seed < 25; seed++)
            {
                var simple = expression.ParseRoll(new RandomIntGenerator(seed));
                var detailed = expression.ParseRollDetailed(new RandomIntGenerator(seed));

                Assert.That(detailed.Value, Is.EqualTo(simple),
                    $"'{expression}' (seed {seed}): Evaluate and EvaluateDetailed must agree");
            }
        }

        #endregion

        #region Shorthand evaluation

        [Test]
        public void BareExplode_EvaluatesAsExplodeOnMaxFace()
        {
            // 3d6!: 6 explodes -> 2; total 6+3+4+2 = 15 (same as "3d6!=6")
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 6, 3, 4, 2 });
            var result = "3d6!".ParseRoll(rng);
            Assert.That(result, Is.EqualTo(15));
        }

        [Test]
        public void DoubleBangCompound_EvaluatesAsCompoundOnMaxFace()
        {
            // 2d6!!: first die compounds 6 -> 6 -> 3 = 15, second die 4; total 19 (same as "2d6^=6")
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 6, 6, 3, 4 });
            var result = "2d6!!".ParseRoll(rng);
            Assert.That(result, Is.EqualTo(19));
        }

        [Test]
        public void BareCaretCompound_EvaluatesAsCompoundOnMaxFace()
        {
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 6, 5, 4 });
            var result = "1d6^".ParseRoll(rng);
            Assert.That(result, Is.EqualTo(11)); // 6 compounds -> 5 (stops), total 11
        }

        [Test]
        public void RerollShorthand_EvaluatesLikeExplicitEquality()
        {
            // 4d6ro1: the 1 is rerolled once -> 5; total 5+4+6+3 = 18
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 1, 4, 6, 3, 5 });
            var result = "4d6ro1".ParseRoll(rng);
            Assert.That(result, Is.EqualTo(18));
        }

        [Test]
        public void ShorthandAndExplicitForms_ProduceSameResults()
        {
            var pairs = new (string shorthand, string explicitForm)[]
            {
                ("3d6!", "3d6!=6"),
                ("2d6!!", "2d6^=6"),
                ("2d6^", "2d6^=6"),
                ("4d6ro1", "4d6ro=1"),
                ("4d6rc2", "4d6rc=2"),
                ("2d10!10", "2d10!=10"),
            };

            foreach (var (shorthand, explicitForm) in pairs)
            {
                for (int seed = 0; seed < 10; seed++)
                {
                    var shorthandResult = shorthand.ParseRoll(new RandomIntGenerator(seed));
                    var explicitResult = explicitForm.ParseRoll(new RandomIntGenerator(seed));

                    Assert.That(shorthandResult, Is.EqualTo(explicitResult),
                        $"'{shorthand}' and '{explicitForm}' (seed {seed}) must be equivalent");
                }
            }
        }

        [Test]
        public void BareExplodeWithArithmetic_DoesNotConsumeConstantTerm()
        {
            // 2d6!+3: 6 explodes -> 4; total 6+2+4+3 = 15
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 6, 2, 4 });
            var result = "2d6!+3".ParseRoll(rng);
            Assert.That(result, Is.EqualTo(15));
        }

        #endregion

        #region Parse error diagnostics

        [Test]
        public void ParseRoll_InvalidInput_ExceptionCarriesDiagnostic()
        {
            var ex = Assert.Throws<FormatException>(() => "2d6xx".ParseRoll());
            Assert.That(ex.Message, Does.StartWith("Invalid roll format."));
            Assert.That(ex.Message.Length, Is.GreaterThan("Invalid roll format.".Length),
                "The exception message should include the parser diagnostic");
        }

        [Test]
        public void ParseRollDetailed_InvalidInput_ExceptionCarriesDiagnostic()
        {
            var ex = Assert.Throws<FormatException>(() => "d6+".ParseRollDetailed());
            Assert.That(ex.Message, Does.StartWith("Invalid roll format."));
        }

        #endregion
    }
}
