using System.Buffers;
using Pidgin;
using static Pidgin.Parser<char>;
using static Pidgin.Parser;

namespace DotDice.Parser
{
    public record ComparisonPoint(ComparisonOperator compOp, int value);

    public static class DiceParser
    {
        static Parser<char, int> PositiveInt =
            DecimalNum.Where(x => x > 0);

        static Parser<char, T> Tok<T>(Parser<char, T> p)
            => SkipWhitespaces.Then(Try(p).Before(SkipWhitespaces));

        // Parser for comparison operators
        public static readonly Parser<char, ComparisonOperator> comparisonOperator =
            Tok(
                Char('=')
                    .ThenReturn(ComparisonOperator.Equal)
                .Or(Char('>')
                    .ThenReturn(ComparisonOperator.GreaterThan))
                .Or(Char('<')
                    .ThenReturn(ComparisonOperator.LessThan))
            );

        // Parser for comparison point
        public static readonly Parser<char, ComparisonPoint> comparisonPoint =
            Tok(
                comparisonOperator
                .Then(
                    PositiveInt, (op, val) => new ComparisonPoint(op, val)
                )
            );

        // Parser for success modifier
        public static readonly Parser<char, Modifier> successModifier =
            Tok(
                comparisonPoint.Map(cp => (Modifier)new SuccessModifier(cp.compOp, cp.value))
            );

        // Parser for failure modifier
        public static readonly Parser<char, Modifier> failureModifier =
            Tok(
                Char('f')
                .Then(
                    comparisonPoint,
                    (_, cp) => (Modifier)new FailureModifier(cp.compOp, cp.value)
                )
            );

        // Parser for explode modifier
        public static readonly Parser<char, Modifier> explodeModifier =
            Tok(
                Char('!').
                Then(
                    comparisonPoint,
                    (_, cp) => (Modifier)new ExplodeModifier(cp.compOp, cp.value)
                )
            );

        // Parser for compounding modifier
        public static readonly Parser<char, Modifier> compoundingModifier =
            Tok(
                Char('^').
                Then(
                    comparisonPoint,
                    (_, cp) => (Modifier)new CompoundingModifier(cp.compOp, cp.value)
                )
            );

        // Parser for reroll modifier
        public static readonly Parser<char, Modifier> rerollOnceModifier =
            Tok(
                String("ro")
                .Then(
                    comparisonPoint,
                    (_, cp) => (Modifier)new RerollOnceModifier(cp.compOp, cp.value)
                )
            );

        // Parser for reroll modifier
        public static readonly Parser<char, Modifier> rerollCompoundModifier =
            Tok(
                String("rc")
                .Then(
                    comparisonPoint,
                    (_, cp) => (Modifier)new RerollMultipleModifier(cp.compOp, cp.value)
                )
            );

        // Parser for keep modifier
        public static readonly Parser<char, Modifier> keepHighModifier =
            Tok(
                String("kh")
                .Then(
                    PositiveInt.Optional(),
                    (_, maybeVal) =>
                    {
                        var val = maybeVal.HasValue ? maybeVal.Value : 1;
                        return (Modifier)new KeepModifier(val, true);
                    }
                )
            );

        // Parser for keep low modifier
        public static readonly Parser<char, Modifier> keepLowModifier =
            Tok(
               String("kl")
                .Then(
                    PositiveInt.Optional(),
                    (_, maybeVal) =>
                    {
                        var val = maybeVal.HasValue ? maybeVal.Value : 1;
                        return (Modifier)new KeepModifier(val, false);
                    }
                )
            );

        // Parser for drop high modifier
        public static readonly Parser<char, Modifier> dropHighModifier =
            Tok(
                String("dh")
                .Then(
                    PositiveInt.Optional(),
                    (_, maybeVal) =>
                    {
                        var val = maybeVal.HasValue ? maybeVal.Value : 1;
                        return (Modifier)new DropModifier(val, true);
                    }
                )
            );

        // Parser for drop low modifier
        public static readonly Parser<char, Modifier> dropLowModifier =
            Tok(
                String("dl")
                .Then(
                    PositiveInt.Optional(),
                    (_, maybeVal) =>
                    {
                        var val = maybeVal.HasValue ? maybeVal.Value : 1;
                        return (Modifier)new DropModifier(val, false);
                    }
                )
            );

        public static readonly Parser<char, Modifier> constantModifier =
            Tok(
                Char('+')
                .Or(Char('-'))
                .Then(
                    PositiveInt,
                    (opChar, val) =>
                    {
                        var op = opChar == '+' ? ArithmaticOperator.Add : ArithmaticOperator.Subtract;
                        return (Modifier)new ConstantModifier(op, val);
                    }
                )
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
                .Or(constantModifier);

        // Parser for multiple modifiers
        public static readonly Parser<char, IEnumerable<Modifier>> Modifiers = Modifier.Many();

        // Parser for a constant
        public static readonly Parser<char, Roll> Constant =
            Tok(
                PositiveInt.Select(x => (Roll)new Constant(x))
                .Before(SkipWhitespaces)
            );

        // Parser for a die type section
        public static readonly Parser<char, DieType> dieTypeSection =
            Tok(
                Char('F')
                    .Map(_ => (DieType)new DieType.Fudge())
                .Or(Char('%')
                    .Map(_ => (DieType)new DieType.Percent()))
                .Or(PositiveInt
                    .Map(val => (DieType)new DieType.Basic(val)))
            );

        // Parser for a basic roll
        public static readonly Parser<char, Roll> basicRoll =
            Tok(
                Map(
                    (maybeNum, _, die, mod) =>
                        (Roll)new BasicRoll(
                            maybeNum.HasValue ? maybeNum.Value : 1,
                            die,
                            mod
                        )
                    ,
                    PositiveInt.Optional(),
                    Char('d'),
                    dieTypeSection,
                    Modifiers
                )
                .Before(SkipWhitespaces)
            );

        // Parser for arithmetic operators in roll expressions
        public static readonly Parser<char, ArithmeticOperator> arithmeticOperator =
            Tok(
                Char('+')
                    .ThenReturn(ArithmeticOperator.Add)
                .Or(Char('-')
                    .ThenReturn(ArithmeticOperator.Subtract))
            );

        // Parser for a single modifier (excluding constant modifiers for arithmetic expressions)
        public static readonly Parser<char, Modifier> ModifierExcludingConstant =
            successModifier
                .Or(failureModifier)
                .Or(dropHighModifier)
                .Or(dropLowModifier)
                .Or(keepHighModifier)
                .Or(keepLowModifier)
                .Or(rerollOnceModifier)
                .Or(rerollCompoundModifier)
                .Or(explodeModifier)
                .Or(compoundingModifier);

        // Parser for multiple modifiers (excluding constant modifiers)
        public static readonly Parser<char, IEnumerable<Modifier>> ModifiersExcludingConstant = ModifierExcludingConstant.Many();

        // Parser for a basic roll without constant modifiers (for use in arithmetic expressions)
        public static readonly Parser<char, Roll> basicRollWithoutConstant =
            Tok(
                Map(
                    (maybeNum, _, die, mod) =>
                        (Roll)new BasicRoll(
                            maybeNum.HasValue ? maybeNum.Value : 1,
                            die,
                            mod
                        )
                    ,
                    PositiveInt.Optional(),
                    Char('d'),
                    dieTypeSection,
                    ModifiersExcludingConstant
                )
                .Before(SkipWhitespaces)
            );

        // Parser for a single roll term (basic roll without constant or constant)
        public static readonly Parser<char, Roll> rollTerm =
            Tok(basicRollWithoutConstant.Or(Constant));

        // Parser for arithmetic roll expressions
        public static readonly Parser<char, Roll> arithmeticRoll =
            Map(
                (firstRoll, additionalTerms) =>
                {
                    if (!additionalTerms.Any())
                    {
                        // Single term, return it as-is
                        return firstRoll;
                    }
                    
                    // Multiple terms, create an ArithmeticRoll
                    var terms = new List<(ArithmaticOperator, Roll)>
                    {
                        (ArithmaticOperator.Add, firstRoll) // First term is implicitly positive
                    };
                    terms.AddRange(additionalTerms);
                    return (Roll)new ArithmeticRoll(terms);
                },
                rollTerm,
                Map(
                    (op, roll) => (op, roll),
                    arithmeticOperator,
                    rollTerm
                ).Many()
            );

        // Top-level parser for any roll
        public static readonly Parser<char, Roll> Roll =
            SkipWhitespaces.Then(
                Try(arithmeticRoll)
                .Or(basicRoll)
                .Or(Constant)
                .Before(SkipWhitespaces)  // Skip any trailing whitespace
                .Before(End)
        );
    }
}
