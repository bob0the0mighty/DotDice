using System.Runtime.CompilerServices;
using Pidgin;
using static Pidgin.Parser;

namespace DotDice
{
    public static class DiceParser
    {
        // Parser for comparison operators
        public static readonly Parser<char, ComparisonOperator> comparisonOperator =
            Char('=')
                .ThenReturn(ComparisonOperator.Equal)
            .Or(Char('>')
                .ThenReturn(ComparisonOperator.GreaterThan))
            .Or(Char('<')
                .ThenReturn(ComparisonOperator.LessThan));

        // Parser for sort directions
        public static readonly Parser<char, SortDirection> sortDirection =
            Char('a')
                .ThenReturn(SortDirection.Ascending)
            .Or(Char('d')
                .ThenReturn(SortDirection.Descending));

        // Parser for sort modifier
        public static readonly Parser<char, Modifier> sortModifier =
            String("sa")
                .ThenReturn((Modifier)new SortModifier(SortDirection.Ascending))
            .Or(String("sd")
                .ThenReturn((Modifier)new SortModifier(SortDirection.Descending)));

        // Parser for success modifier
        public static readonly Parser<char, Modifier> successModifier =
            Map(
                (op, value) => (Modifier)new SuccessModifier(op, value),
                comparisonOperator,
                Num
            );

        // Parser for failure modifier
        public static readonly Parser<char, Modifier> failureModifier =
            Map(
                (op, value) => (Modifier)new FailureModifier(op, value),
                Char('f').Then(comparisonOperator),
                Num
            );

        // Parser for explode modifier
        public static readonly Parser<char, Modifier> explodeModifier =
            Map(
                (cOp, value) =>
                    {
                        return (Modifier)new ExplodeModifier(cOp, value);
                    },
                Char('!').Then(comparisonOperator),
                Num
            );

        // Parser for compounding modifier
        public static readonly Parser<char, Modifier> compoundingModifier =
            Map(
                (op, value) =>
                    {
                        return (Modifier)new CompoundingModifier(op, value);
                    },
                String("!!").Then(comparisonOperator),
                Num
            );


        // Parser for reroll modifier
        public static readonly Parser<Modifier> rerollModifier =
            Char('r')
                .Then(Char('o').Optional())
                .SelectMany(_ => comparisonOperator, (ro, op) => (ro, op))
                .SelectMany(
                    _ => Integer,
                    (tup, value) =>
                    {
                        var (onlyOnce, cOp) = tup;
                        return (Modifier)new RerollModifier(cOp, value, onlyOnce.HasValue);
                    });

        // Parser for keep modifier
        public static readonly Parser<Modifier> keepModifier =
            String("kh")
                .SelectMany(
                    _ => OptionalInteger,
                    (_, count) =>
                    {
                        return (Modifier)new KeepModifier(count.HasValue ? count.Value : 1, true);
                    })
            .Or(
                String("kl")
                .SelectMany(
                    _ => OptionalInteger,
                    (_, count) =>
                    {
                        return (Modifier)new KeepModifier(count.HasValue ? count.Value : 1, false);
                    }));

        // Parser for drop modifier
        public static readonly Parser<Modifier> dropModifier =
            String("dh")
                .SelectMany(
                    _ => OptionalInteger,
                    (_, count) =>
                    {
                        return (Modifier)new DropModifier(count.HasValue ? count.Value : 1, false);
                    })
            .Or(
                String("dl")
                .SelectMany(
                    _ => OptionalInteger,
                    (_, count) =>
                    {
                        return (Modifier)new DropModifier(count.HasValue ? count.Value : 1, true);
                    }));

        // Parser for a single modifier
        public static readonly Parser<Modifier> Modifier =
            keepModifier
                .Or(dropModifier)
                .Or(rerollModifier)
                .Or(explodeModifier)
                .Or(compoundingModifier)
                .Or(successModifier)
                .Or(failureModifier)
                .Or(sortModifier);

        // Parser for multiple modifiers
        public static readonly Parser<List<Modifier>> Modifiers = Modifier.Many();

        // Parser for a constant
        public static readonly Parser<Roll> Constant =
            Integer.Select(x => (Roll)new Constant(x));

        // Parser for a die type section
        public static readonly Parser<(int DieType, bool IsFudgeDie, bool IsPercentile)> dieTypeSection =
            Char('F')
                .ThenReturn((DieType: 0, IsFudgeDie: true, IsPercentile: false))
            .Or(Char('%')
                .ThenReturn((DieType: 0, IsFudgeDie: false, IsPercentile: true)))
            .Or(Integer.Select(dt => (DieType: dt, IsFudgeDie: false, IsPercentile: false)));

        // Parser for a basic roll
        public static readonly Parser<Roll> basicRoll =
            OptionalInteger
                .ThenSkip(Char('d'))
                .SelectMany(_ => dieTypeSection, (numDice, dts) => (numDice, dts.DieType, dts.IsFudgeDie, dts.IsPercentile))
                .SelectMany(
                    _ => Modifiers,
                    (tup, modifiers) =>
                    {
                        var (numDice, dieType, isFudge, isPercentile) = tup;
                        return (Roll)new BasicRoll(numDice.HasValue ? numDice.Value : 1, dieType, isFudge, isPercentile, modifiers);
                    });

        // Parser for a roll group (either a basic roll or a constant)
        public static readonly Parser<Roll> RollGroup = basicRoll.Or(Constant);

        // Parser for a grouped roll
        public static readonly Parser<Roll> groupedRoll =
    Char('{')
        .Then(RollGroup.ThenSkip(Optional(Char(','))).Many())
        .ThenSkip(Char('}'))
        .SelectMany(
            rolls => Modifiers,
            (rolls, modifiers) => (Roll)new GroupedRoll(rolls, modifiers));

        // Top-level parser for any roll
        public static readonly Parser<Roll> Roll = groupedRoll.Or(basicRoll);
    }
}
