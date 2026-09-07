using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotDice.Evaluator;
using DotDice.Parser;
using NUnit.Framework;
using Pidgin;
using static DotDice.Tests.TestHelpers;

namespace DotDice.Tests
{
    /// <summary>
    /// The properties the evaluator must keep while its internals are rewritten for
    /// allocation.
    ///
    /// These are deliberately not tests of what any particular expression evaluates
    /// to, since the existing suite covers that. They pin the things a rewrite can break
    /// without any existing test noticing: that the simple and detailed paths stay
    /// one set of semantics, that randomness is drawn in exactly the same way, and
    /// that keep/drop tie-breaking stays stable.
    /// </summary>
    [TestFixture]
    public class EvaluatorContractTests
    {
        private static Roll Parse(string expression)
        {
            var parsed = DiceParser.Roll.Parse(expression);
            Assert.That(parsed.Success, Is.True, $"corpus expression failed to parse: {expression}");
            return parsed.Value;
        }

        private static IEnumerable<EvaluatorCase> Corpus() => EvaluatorCorpus.Cases();

        /// <summary>
        /// Evaluate is documented as a projection of EvaluateDetailed. Once it stops
        /// materialising events it is no longer literally that, and this is what keeps
        /// the two from drifting. That drift is the failure mode that produced the
        /// reroll and success/failure bugs the last review found.
        /// </summary>
        [TestCaseSource(nameof(Corpus))]
        public void SimpleAndDetailedPathsAgreeOnValue(EvaluatorCase testCase)
        {
            var roll = Parse(testCase.Expression);

            var simple = new DiceEvaluator(new RecordingRandomNumberGenerator(testCase.Script)).Evaluate(roll);
            var detailed = new DiceEvaluator(new RecordingRandomNumberGenerator(testCase.Script)).EvaluateDetailed(roll).Value;

            Assert.That(simple, Is.EqualTo(detailed),
                $"{testCase.Expression} evaluates to {simple} through Evaluate but {detailed} through EvaluateDetailed");
        }

        /// <summary>
        /// The same expression must draw the same randomness whichever entry point is
        /// used. A fast path that skipped a draw would still produce plausible totals
        /// while quietly consuming a different RNG sequence than the detailed path.
        /// </summary>
        [TestCaseSource(nameof(Corpus))]
        public void SimpleAndDetailedPathsConsumeRandomnessIdentically(EvaluatorCase testCase)
        {
            var roll = Parse(testCase.Expression);

            var simpleRng = new RecordingRandomNumberGenerator(testCase.Script);
            new DiceEvaluator(simpleRng).Evaluate(roll);

            var detailedRng = new RecordingRandomNumberGenerator(testCase.Script);
            new DiceEvaluator(detailedRng).EvaluateDetailed(roll);

            Assert.That(simpleRng.Calls, Is.EqualTo(detailedRng.Calls).AsCollection,
                $"{testCase.Expression} draws randomness differently through the two paths");
        }

        /// <summary>
        /// Pins the exact RNG draws against a checked-in transcript: same methods,
        /// same arguments, same order.
        ///
        /// This is the strongest constraint on the rewrite and the easiest to break
        /// silently. Next(1, sides + 1) and Next(sides) + 1 cover the same range but
        /// map differently inside System.Random, so swapping one for the other
        /// changes what a given seed produces without changing any total's validity.
        /// A consumer simulating its own RNG would then be observing a path the real
        /// roll never takes.
        ///
        /// Regenerate with RegenerateRngCallTranscripts below, and read the resulting
        /// diff as a behaviour change that needs justifying, not as noise.
        /// </summary>
        [TestCaseSource(nameof(Corpus))]
        public void RngCallSequenceMatchesTranscript(EvaluatorCase testCase)
        {
            var transcripts = LoadTranscripts();
            var key = TranscriptKey(testCase);

            Assert.That(transcripts.ContainsKey(key), Is.True,
                $"no recorded transcript for {key}. Run RegenerateRngCallTranscripts and review the diff.");

            var rng = new RecordingRandomNumberGenerator(testCase.Script);
            new DiceEvaluator(rng).EvaluateDetailed(Parse(testCase.Expression));

            Assert.That(RenderDraws(rng.Calls), Is.EqualTo(transcripts[key]),
                $"{testCase.Expression} no longer draws randomness the way it did");
        }

        /// <summary>
        /// Every recorded transcript still corresponds to a live corpus entry, so
        /// removing a case from the corpus cannot leave a stale transcript behind
        /// giving false confidence.
        /// </summary>
        [Test]
        public void TranscriptFileHasNoStaleEntries()
        {
            var live = Corpus().Select(TranscriptKey).ToHashSet();
            var stale = LoadTranscripts().Keys.Where(k => !live.Contains(k)).ToList();

            Assert.That(stale, Is.Empty,
                "transcripts exist for corpus entries that no longer exist: " + string.Join(", ", stale));
        }

        /// <summary>
        /// Which of two equal dice is marked Dropped is observable: a consumer renders
        /// dropped dice struck through, so an unstable sort is a silent cosmetic
        /// regression downstream rather than a wrong total.
        ///
        /// Selection must be stable in original roll order. Of three equal 5s in
        /// 4d6kh2, the first two are kept.
        /// </summary>
        [Test]
        public void KeepHighestBreaksTiesInRollOrder()
        {
            var rng = new RecordingRandomNumberGenerator(new[] { 5, 5, 5, 1 });
            var result = new DiceEvaluator(rng).EvaluateDetailed(Parse("4d6kh2"));

            Assert.That(result.Events.Select(e => (e.Value, e.Status)), Is.EqualTo(new[]
            {
                (5, DieStatus.Kept),
                (5, DieStatus.Kept),
                (5, DieStatus.Dropped),
                (1, DieStatus.Dropped)
            }).AsCollection);
        }

        /// <summary>
        /// The same property for drop-lowest: of four equal 2s in 6d6dl3, the first
        /// three are the ones dropped.
        /// </summary>
        [Test]
        public void DropLowestBreaksTiesInRollOrder()
        {
            var rng = new RecordingRandomNumberGenerator(new[] { 2, 2, 2, 2, 5, 6 });
            var result = new DiceEvaluator(rng).EvaluateDetailed(Parse("6d6dl3"));

            Assert.That(result.Events.Select(e => (e.Value, e.Status)), Is.EqualTo(new[]
            {
                (2, DieStatus.Dropped),
                (2, DieStatus.Dropped),
                (2, DieStatus.Dropped),
                (2, DieStatus.Kept),
                (5, DieStatus.Kept),
                (6, DieStatus.Kept)
            }).AsCollection);
        }

        /// <summary>
        /// Keep-lowest with a tie spanning the selection boundary: three 2s compete
        /// for two slots, and it is the first two that survive.
        /// </summary>
        [Test]
        public void KeepLowestBreaksTiesInRollOrder()
        {
            var rng = new RecordingRandomNumberGenerator(new[] { 2, 5, 2, 2, 7 });
            var result = new DiceEvaluator(rng).EvaluateDetailed(Parse("5d10kl2"));

            Assert.That(result.Events.Select(e => (e.Value, e.Status)), Is.EqualTo(new[]
            {
                (2, DieStatus.Kept),
                (5, DieStatus.Dropped),
                (2, DieStatus.Kept),
                (2, DieStatus.Dropped),
                (7, DieStatus.Dropped)
            }).AsCollection);
        }

        #region Transcript storage

        private const string TranscriptFileName = "rng-call-transcripts.txt";

        /// <summary>
        /// Renders a draw sequence, collapsing consecutive identical draws to "call*N".
        ///
        /// A hundred-die pool draws a hundred identical calls, and a loop guard draws a
        /// hundred more; spelling each one out makes the transcript unreadable and its
        /// diffs useless. The collapsed form still distinguishes every difference that
        /// matters: a changed argument, a changed order, a changed count.
        /// </summary>
        private static string RenderDraws(IReadOnlyList<string> calls)
        {
            var parts = new List<string>();

            for (int i = 0; i < calls.Count;)
            {
                int run = 1;
                while (i + run < calls.Count && calls[i + run] == calls[i])
                {
                    run++;
                }

                parts.Add(run == 1 ? calls[i] : $"{calls[i]}*{run}");
                i += run;
            }

            return string.Join(" ", parts);
        }

        private static string TranscriptKey(EvaluatorCase testCase)
            => $"{testCase.Expression}|{string.Join(",", testCase.Script)}";

        private static string TranscriptPath()
            => Path.Combine(TestContext.CurrentContext.TestDirectory, "data", TranscriptFileName);

        private static Dictionary<string, string> LoadTranscripts()
        {
            var path = TranscriptPath();
            Assert.That(File.Exists(path), Is.True, $"transcript file missing: {path}");

            return File.ReadAllLines(path)
                .Where(line => line.Length > 0 && !line.StartsWith("#"))
                .Select(line => line.Split('|'))
                .ToDictionary(parts => $"{parts[0]}|{parts[1]}", parts => parts[2]);
        }

        /// <summary>
        /// Rewrites the checked-in transcript file from the current evaluator.
        ///
        /// Explicit because running it turns a failing contract into a passing one.
        /// Run it only when a change to RNG draw order is intended, and review the
        /// resulting diff line by line.
        /// </summary>
        [Test, Explicit("Rewrites the checked-in RNG transcripts; run deliberately and review the diff.")]
        public void RegenerateRngCallTranscripts()
        {
            var lines = new List<string>
            {
                "# RNG draw transcripts, one per corpus entry.",
                "# Format: expression|script|draws",
                "# Generated by EvaluatorContractTests.RegenerateRngCallTranscripts.",
                "# A diff here is a change in how the evaluator consumes randomness.",
                ""
            };

            foreach (var testCase in Corpus())
            {
                var rng = new RecordingRandomNumberGenerator(testCase.Script);
                new DiceEvaluator(rng).EvaluateDetailed(Parse(testCase.Expression));
                lines.Add($"{TranscriptKey(testCase)}|{RenderDraws(rng.Calls)}");
            }

            var sourcePath = Path.Combine(FindTestProjectRoot(), "data", TranscriptFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(sourcePath)!);
            File.WriteAllLines(sourcePath, lines);
            TestContext.Out.WriteLine($"wrote {lines.Count - 5} transcripts to {sourcePath}");
        }

        private static string FindTestProjectRoot()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "DotDiceTests.csproj")))
            {
                dir = dir.Parent;
            }

            Assert.That(dir, Is.Not.Null, "could not locate DotDiceTests.csproj above the test directory");
            return dir!.FullName;
        }

        #endregion
    }
}
