namespace DotDice.Parser
{
    // Enums for ComparisonOperator and SortDirection
    public enum ComparisonOperator
    {
        Equal,
        GreaterThan,
        LessThan
    }

    public enum ArithmeticOperator
    {
        Add,
        Subtract
    }

    public enum SortDirection
    {
        Ascending,
        Descending
    }

    // Event-driven model enums
    public enum DieEventType 
    { 
        Initial, 
        Reroll, 
        Explosion, 
        Compound 
    }

    public enum DieStatus 
    { 
        Kept, 
        Dropped, 
        Discarded 
    }

    public enum RollSignificance 
    { 
        None, 
        Minimum, 
        Maximum 
    }

    public enum SuccessStatus 
    { 
        Neutral, 
        Success, 
        Failure 
    }

    public record DieType
    {
        public record Basic(int sides) : DieType;
        public record Percent() : DieType;
        public record Fudge() : DieType;

        [Obsolete(RerolledDiceKeepTheirDieType)]
        public record Reroll(int sides) : DieType;

        [Obsolete(SyntheticEventsHaveNoDieType)]
        public record Constant() : DieType;

        [Obsolete(SyntheticEventsHaveNoDieType)]
        public record Success() : DieType;

        [Obsolete(RerolledDiceKeepTheirDieType)]
        public record Explode(int sides) : DieType;

        // The evaluator only ever produces Basic, Percent and Fudge. The four records
        // above have never appeared in a DieEvent, so a consumer matching on them is
        // writing dead code that looks correct. They are deprecated rather than deleted
        // because deleting a public type is a breaking change; 2.0 removes them.
        private const string RerolledDiceKeepTheirDieType =
            "Never produced. A rerolled or exploded die keeps the DieType it was rolled with; use DieEvent.Type to tell how an event was generated. Removed in 2.0.";

        private const string SyntheticEventsHaveNoDieType =
            "Never produced. Success counts and constant modifiers appear as events with a null DieType. Removed in 2.0.";
    }
}