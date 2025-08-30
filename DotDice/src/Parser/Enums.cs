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
        public record Reroll(int sides) : DieType;
        public record Constant() : DieType;
        public record Success() : DieType;
        public record Explode(int sides) : DieType;
    }
}