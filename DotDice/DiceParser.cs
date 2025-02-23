using System.Runtime.CompilerServices;
using Pidgin;
using static Pidgin.Parser;

namespace DotDice
{
    public record ComparisonPoint(ComparisonOperator compOp, int value);

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

        // Parser for comparison point
        public static readonly Parser<char, ComparisonPoint> comparisonPoint =
            Try(
                comparisonOperator
                .Then(
                    DecimalNum, (op, val) => new ComparisonPoint(op, val)
                )
            ) 
            .Or(
                DecimalNum .Map(val => new ComparisonPoint(ComparisonOperator.Equal, val))
            );

        // Parser for sort modifier
        public static readonly Parser<char, Modifier> sortModifier =
            String("sa")
                .ThenReturn((Modifier)new SortModifier(SortDirection.Ascending))
            .Or(String("sd")
                .ThenReturn((Modifier)new SortModifier(SortDirection.Descending)));

        // Parser for success modifier
        public static readonly Parser<char, Modifier> successModifier =
           comparisonPoint.Map(cp => (Modifier) new SuccessModifier(cp.compOp, cp.value));

        // Parser for failure modifier
        public static readonly Parser<char, Modifier> failureModifier =
            Char('f')
                .Then(
                    comparisonPoint,
                    (_, cp) => (Modifier) new FailureModifier(cp.compOp, cp.value)
                );

        // Parser for explode modifier
        public static readonly Parser<char, Modifier> explodeModifier =
            Char('!').
                Then(
                    comparisonPoint,
                    (_, cp) => (Modifier) new ExplodeModifier(cp.compOp, cp.value)
            );

        // Parser for compounding modifier
        public static readonly Parser<char, Modifier> compoundingModifier =
            Char('^').
                Then(
                    comparisonPoint,
                    (_, cp) => (Modifier) new CompoundingModifier(cp.compOp, cp.value)
            );

        // Parser for reroll modifier
        public static readonly Parser<char, Modifier> rerollOnceModifier =
            String("ro")
                .Then(
                    comparisonPoint,
                    (_, cp) => (Modifier) new RerollOnceModifier(cp.compOp, cp.value)
                );

        // Parser for reroll modifier
        public static readonly Parser<char, Modifier> rerollCompoundModifier =
            String("rc")
                .Then(
                    comparisonPoint,
                    (_, cp) => (Modifier) new RerollCompoundModifier(cp.compOp, cp.value)
                );

        // Parser for keep modifier
        public static readonly Parser<char, Modifier> keepHighModifier =
            String("kh")
                .Then(
                    DecimalNum.Optional(),
                    (_,maybeVal) => 
                    {
                        var val = maybeVal.HasValue ? int.Abs(maybeVal.Value) : 1;
                        return (Modifier) new KeepModifier(val, true);
                    }
                );
            
        // Parser for keep low modifier
        public static readonly Parser<char, Modifier> keepLowModifier =
            String("kl")
                .Then(
                    DecimalNum.Optional(),
                    (_,maybeVal) => 
                    {
                        var val = maybeVal.HasValue ? int.Abs(maybeVal.Value) : 1;
                        return (Modifier) new KeepModifier(val, false);
                    }
                );

        // Parser for drop high modifier
        public static readonly Parser<char, Modifier> dropHighModifier =
            String("dh")
                .Then(
                    DecimalNum.Optional(),
                    (_,maybeVal) => 
                    {
                        var val = maybeVal.HasValue ? int.Abs(maybeVal.Value) : 1;
                        return (Modifier) new DropModifier(val, true);
                    }
                );

        // Parser for drop low modifier
        public static readonly Parser<char, Modifier> dropLowModifier = 
            String("dl")
                .Then(
                    DecimalNum.Optional(),
                    (_,maybeVal) => 
                    {
                        var val = maybeVal.HasValue ? int.Abs(maybeVal.Value) : 1;
                        return (Modifier) new DropModifier(val, false);
                    }
                );

        // Parser for a single modifier
        public static readonly Parser<char, Modifier> Modifier =
            successModifier
                .Or(failureModifier)
                .Or(dropHighModifier)
                .Or(dropLowModifier)
                .Or(keepHighModifier)
                .Or(keepLowModifier)
                .Or(rerollOnceModifier)
                .Or(rerollCompoundModifier)
                .Or(explodeModifier)
                .Or(compoundingModifier)
                .Or(sortModifier);

        // Parser for multiple modifiers
        public static readonly Parser<char, IEnumerable<Modifier>> Modifiers = Modifier.Many();

        // Parser for a constant
        public static readonly Parser<char, Roll> Constant =
            DecimalNum.Select(x => (Roll) new Constant(x));

        // Parser for a die type section
        public static readonly Parser<char, DieType> dieTypeSection =
            Char('F')
                .Map(_ => (DieType) new DieType.Fudge())
            .Or(Char('%')
                .Map(_ => (DieType) new DieType.Percent()))
            .Or(DecimalNum
                .Map(val => (DieType) new DieType.Basic(val)));

        // Parser for a basic roll
        public static readonly Parser<char, Roll> basicRoll =
            Map(
                (maybeNum, _, die, mod) => 
                    (Roll) new BasicRoll(
                        maybeNum.HasValue ? maybeNum.Value : 1,
                        die,
                        mod
                    )
                ,
                DecimalNum.Optional(),
                Char('d'),
                dieTypeSection,
                Modifiers
            );

        // Top-level parser for any roll
        public static readonly Parser<char, Roll> Roll = basicRoll.Or(Constant);
    }
}
