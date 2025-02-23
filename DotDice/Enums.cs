namespace DotDice
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;

    // Enums for ComparisonOperator and SortDirection
    public enum ComparisonOperator
    {
        Equal,
        GreaterThan,
        LessThan
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
        public record Fudge(): DieType;
    }
}