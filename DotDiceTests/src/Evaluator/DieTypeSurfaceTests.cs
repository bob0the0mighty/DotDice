using System.Reflection;
using DotDice.Evaluator;
using DotDice.Parser;
using Pidgin;
using static DotDice.Tests.TestHelpers;

namespace DotDice.Tests
{
    /// <summary>
    /// DieType.Reroll, DieType.Explode, DieType.Success and DieType.Constant are marked
    /// obsolete on the grounds that the evaluator never produces one. These tests are what
    /// makes that claim checkable rather than a comment: if a future change starts emitting
    /// one, the deprecation is wrong and the suite says so.
    /// </summary>
    [TestFixture]
    public class DieTypeSurfaceTests
    {
        private static IEnumerable<EvaluatorCase> Corpus() => EvaluatorCorpus.Cases();

        [TestCaseSource(nameof(Corpus))]
        public void NoEventCarriesADeprecatedDieType(EvaluatorCase testCase)
        {
            var parsed = DiceParser.Roll.Parse(testCase.Expression);
            Assert.That(parsed.Success, Is.True, $"corpus expression failed to parse: {testCase.Expression}");

            var result = new DiceEvaluator(new CyclingRandomNumberGenerator(testCase.Script))
                .EvaluateDetailed(parsed.Value);

            foreach (var evt in result.Events)
            {
                Assert.That(
                    evt.DieType is null or DieType.Basic or DieType.Percent or DieType.Fudge,
                    Is.True,
                    $"{testCase.Expression} produced a {evt.DieType}, which is deprecated as never produced");
            }
        }

        [Test]
        public void OnlyTheProducedDieTypesAreCurrent()
        {
            var current = typeof(DieType)
                .GetNestedTypes(BindingFlags.Public)
                .Where(nested => nested.GetCustomAttribute<ObsoleteAttribute>() == null)
                .Select(nested => nested.Name)
                .OrderBy(name => name);

            Assert.That(current, Is.EqualTo(new[] { "Basic", "Fudge", "Percent" }).AsCollection);
        }
    }
}
