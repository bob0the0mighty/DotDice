using DotDice.Extension;
using DotDice.Parser;

namespace DotDice.Tests
{
    /// <summary>
    /// Success and failure counting used to replace the whole event list with a single
    /// count, so a caller could see how many dice succeeded but never which ones. The
    /// counted dice are now kept as Discarded events carrying the status they earned,
    /// matching what compounding already did with its intermediate rolls.
    ///
    /// The value is unchanged either way, so these are the only tests that can catch a
    /// regression here.
    /// </summary>
    [TestFixture]
    public class SuccessFailureEventTests
    {
        [Test]
        public void CountedDiceSurviveAsDiscardedEvents()
        {
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 8, 1, 9 });
            var result = "3d10>7".ParseRollDetailed(rng);

            Assert.That(result.Value, Is.EqualTo(2));
            Assert.That(result.Events.Count, Is.EqualTo(4));
            Assert.That(
                result.Events.Take(3).Select(e => e.Value),
                Is.EqualTo(new[] { 8, 1, 9 }).AsCollection);
            Assert.That(
                result.Events.Take(3).Select(e => e.Status),
                Is.All.EqualTo(DieStatus.Discarded),
                "A counted die must not also contribute its face value to the total");
        }

        [Test]
        public void CountedDiceCarryTheStatusTheyEarned()
        {
            // ">7" succeeds on 8 and 9; "f=1" fails on the 1; the 3 matches neither.
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 8, 1, 9, 3 });
            var result = "4d10>7f=1".ParseRollDetailed(rng);

            Assert.That(result.Value, Is.EqualTo(1), "2 successes - 1 failure");
            Assert.That(
                result.Events.Take(4).Select(e => e.Success),
                Is.EqualTo(new[]
                {
                    SuccessStatus.Success,
                    SuccessStatus.Failure,
                    SuccessStatus.Success,
                    SuccessStatus.Neutral
                }).AsCollection);
        }

        [Test]
        public void CountEventIsLastAndIsNotADie()
        {
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 8, 9 });
            var result = "2d10>7".ParseRollDetailed(rng);

            var count = result.Events[^1];
            Assert.That(count.Value, Is.EqualTo(2));
            Assert.That(count.Status, Is.EqualTo(DieStatus.Kept));
            Assert.That(count.Success, Is.EqualTo(SuccessStatus.Neutral));
            Assert.That(count.DieType, Is.Null, "The count is synthetic, not a rolled die");
        }

        [Test]
        public void DroppedDiceKeepTheirDroppedStatus()
        {
            // The 2 is dropped before counting, so it is neither counted nor Discarded:
            // a renderer needs to tell "did not count" from "was not rolled with".
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 8, 2, 9 });
            var result = "3d10dl1>7".ParseRollDetailed(rng);

            Assert.That(result.Value, Is.EqualTo(2));
            Assert.That(
                result.Events.Take(3).Select(e => e.Status),
                Is.EqualTo(new[] { DieStatus.Discarded, DieStatus.Dropped, DieStatus.Discarded }).AsCollection);
        }

        [TestCase("4d10>7")]
        [TestCase("4d10f=1")]
        [TestCase("4d10>7f=1")]
        [TestCase("4d10dl1>7")]
        public void BothPathsStillAgreeOnValue(string expression)
        {
            var script = new List<int> { 8, 1, 9, 3 };

            var simple = expression.ParseRoll(new TestHelpers.MockRandomNumberGenerator(script));
            var detailed = expression.ParseRollDetailed(new TestHelpers.MockRandomNumberGenerator(script));

            Assert.That(simple, Is.EqualTo(detailed.Value));
        }
    }
}
