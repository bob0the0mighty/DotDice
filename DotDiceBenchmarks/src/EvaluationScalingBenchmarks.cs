using BenchmarkDotNet.Attributes;
using DotDice.Evaluator;
using DotDice.Parser;
using DotDice.RandomNumberGenerator;
using Pidgin;

namespace DotDiceBenchmarks;

/// <summary>
/// How evaluation scales with dice count, parse excluded.
///
/// <see cref="RollBenchmarks"/> answers "should I hoist the parse out of my loop"
/// and stops at ten dice, where parsing dominates and evaluation barely registers.
/// This one starts where that leaves off: a Monte Carlo run over a large pool
/// parses once and then evaluates a million times, so per-evaluation cost and
/// per-evaluation allocation are the whole story.
///
/// The interesting column is Allocated, not Mean. Every die is a heap-allocated
/// record and each modifier phase copies the list, so bytes grow with the pool
/// while the arithmetic does not.
///
/// Run with: dotnet run -c Release --project DotDiceBenchmarks -- --filter '*Scaling*'
/// Add --job short for a quick look; omit it for numbers worth recording.
/// </summary>
[MemoryDiagnoser]
public class EvaluationScalingBenchmarks
{
    /// <summary>
    /// 500 is not a stress test for its own sake: it is the pool size in the
    /// Monte Carlo run that prompted this work.
    /// </summary>
    [Params(1, 4, 10, 100, 500)]
    public int DiceCount { get; set; }

    private Roll _plain = null!;
    private Roll _keepHalf = null!;
    private DiceEvaluator _evaluator = null!;

    [GlobalSetup]
    public void Setup()
    {
        _plain = ParseOrThrow($"{DiceCount}d20");
        // Keep/drop is the phase with the most work per die (an order plus a
        // per-die status write), so it scales differently from a bare pool.
        _keepHalf = ParseOrThrow($"{DiceCount}d20kh{Math.Max(1, DiceCount / 2)}");
        _evaluator = new DiceEvaluator(new RandomIntGenerator());
    }

    private static Roll ParseOrThrow(string expression)
    {
        var parsed = DiceParser.Roll.Parse(expression);
        if (!parsed.Success)
            throw new FormatException($"benchmark expression failed to parse: {expression}");
        return parsed.Value;
    }

    /// <summary>Total only. What a simulation loop actually calls.</summary>
    [Benchmark(Baseline = true)]
    public int Evaluate() => _evaluator.Evaluate(_plain);

    /// <summary>
    /// The same roll with per-die events materialised. A caller wanting only a
    /// total should not pay this; today it does.
    /// </summary>
    [Benchmark]
    public int EvaluateDetailed() => _evaluator.EvaluateDetailed(_plain).Value;

    /// <summary>Total only, with a keep-highest-half modifier to exercise the ordering path.</summary>
    [Benchmark]
    public int EvaluateKeepHalf() => _evaluator.Evaluate(_keepHalf);
}
