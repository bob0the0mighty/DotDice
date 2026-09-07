using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using DotDice.Evaluator;
using DotDice.Extension;
using DotDice.Parser;
using DotDice.RandomNumberGenerator;
using Pidgin;

namespace DotDiceBenchmarks;

/// <summary>
/// Where the time goes in a Monte Carlo loop.
///
/// The question this exists to answer: a consumer rolling the same expression
/// thousands of times can either call the string extension each time, or parse
/// once and reuse an evaluator. These measure both, plus the parse alone, so the
/// split between parsing and evaluating is visible rather than inferred.
///
/// Both evaluation paths appear separately. <see cref="DiceEvaluator.Evaluate"/>
/// is currently a projection of <see cref="DiceEvaluator.EvaluateDetailed"/>, so
/// they cost the same today; measuring them side by side is what makes it visible
/// when that stops being true.
///
/// Run with: dotnet run -c Release --project DotDiceBenchmarks -- --filter '*RollBenchmarks*'
/// Add --job short for a quick look; omit it for numbers worth recording.
/// </summary>
[MemoryDiagnoser]
public class RollBenchmarks
{
    /// <summary>
    /// Spread deliberately: a bare die, keep-highest, drop-lowest, exploding, and
    /// success counting. If parsing dominates, all five cost about the same
    /// despite rolling between one and ten dice.
    /// </summary>
    [Params("1d20", "2d20kh1", "4d6dl1", "10d6!", "8d10>7")]
    public string Expression { get; set; } = "1d20";

    private Roll _parsedRoll = null!;
    private DiceEvaluator _evaluator = null!;

    [GlobalSetup]
    public void Setup()
    {
        var parsed = DiceParser.Roll.Parse(Expression);
        if (!parsed.Success)
            throw new FormatException($"benchmark expression failed to parse: {Expression}");

        _parsedRoll = parsed.Value;
        _evaluator = new DiceEvaluator(new RandomIntGenerator());
    }

    /// <summary>The parse alone, with no evaluation.</summary>
    [Benchmark]
    public Roll Parse() => DiceParser.Roll.Parse(Expression).Value;

    /// <summary>
    /// The whole string extension: parse, allocate a DiceEvaluator, allocate a
    /// System.Random, evaluate. What a naive per-iteration loop costs.
    /// </summary>
    [Benchmark(Baseline = true)]
    public int ParseAndEvaluate() => Expression.ParseRoll();

    /// <summary>
    /// Evaluation only, against a Roll parsed once and an evaluator reused. The
    /// AST is an immutable record tree and the evaluator holds no per-evaluation
    /// state beyond its RNG, so both hoist out of a loop safely.
    /// </summary>
    [Benchmark]
    public int EvaluatePreParsed() => _evaluator.Evaluate(_parsedRoll);

    /// <summary>
    /// The same, asking for per-die events. A consumer that renders the dice pays
    /// this; one that only wants a total does not, and the gap between this and
    /// <see cref="EvaluatePreParsed"/> is what that choice is worth.
    /// </summary>
    [Benchmark]
    public int EvaluatePreParsedDetailed() => _evaluator.EvaluateDetailed(_parsedRoll).Value;
}

public static class Program
{
    public static void Main(string[] args) =>
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}
