using DotDice.Parser;

namespace DotDice.Evaluator
{
    /// <summary>
    /// Represents a single atomic roll event.
    /// </summary>
    public record DieEvent
    {
        public int Value { get; init; }
        public DieEventType Type { get; init; }
        public DieType? DieType { get; init; }
        public RollSignificance Significance { get; init; }
        public DieStatus Status { get; set; } = DieStatus.Kept;
        public SuccessStatus Success { get; set; } = SuccessStatus.Neutral;
    }

    /// <summary>
    /// Represents the complete result of a dice evaluation, including the final value
    /// and a comprehensive list of all events that occurred during the evaluation.
    /// </summary>
    public class DiceEvaluationResult
    {
        public int Value { get; init; }
        public List<DieEvent> Events { get; }

        public DiceEvaluationResult(int value, List<DieEvent> events)
        {
            Value = value;
            Events = events ?? new List<DieEvent>();
        }
    }
}