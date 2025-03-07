namespace DotDice.Parser
{
    // Enums for ComparisonOperator and SortDirection
    public enum ComparisonOperator
    {
        Equal,
        GreaterThan,
        LessThan
    }

    public enum ArithmaticOperator
    {
        Add,
        Subtract
    }

    public enum SortDirection
    {
        Ascending,
        Descending
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