namespace DotDice
{
    public static class DiceParser
    {
        // Parser for comparison operators
        public static readonly Parser<ComparisonOperator> comparisonOperator =
            ParserCombinators.Char('=')
                .ThenReturn(ComparisonOperator.Equal)
            .Or(ParserCombinators.Char('>')
                .ThenReturn(ComparisonOperator.GreaterThan))
            .Or(ParserCombinators.Char('<')
                .ThenReturn(ComparisonOperator.LessThan));

        // Parser for sort directions
        public static readonly Parser<SortDirection> sortDirection =
            ParserCombinators.Char('a')
                .ThenReturn(SortDirection.Ascending)
            .Or(ParserCombinators.Char('d')
                .ThenReturn(SortDirection.Descending));

        // Parser for sort modifier
        public static readonly Parser<Modifier> sortModifier =
            ParserCombinators.String("sa")
                .ThenReturn((Modifier)new SortModifier(SortDirection.Ascending))
            .Or(ParserCombinators.String("sd")
                .ThenReturn((Modifier)new SortModifier(SortDirection.Descending)));

        // Parser for success modifier
        public static readonly Parser<Modifier> successModifier =
            comparisonOperator
                .SelectMany(
                    op => ParserCombinators.Integer,
                    (op, value) => (Modifier)new SuccessModifier(op, value)
                );

        // Parser for failure modifier
        public static readonly Parser<Modifier> failureModifier =
            ParserCombinators.Char('f')
                .Then(comparisonOperator)
                .SelectMany(
                    op => ParserCombinators.Integer,
                    (op, value) => (Modifier)new FailureModifier(op, value)
                );

        // Parser for explode modifier
        public static readonly Parser<Modifier> explodeModifier =
            ParserCombinators.Char('!')
                .Then(comparisonOperator.Optional())
                .SelectMany(
                    cOp => ParserCombinators.Integer,
                    (cOp, value) =>
                    {
                
                        return (Modifier)new ExplodeModifier(cOp.HasValue ? cOp.Value : ComparisonOperator.Equal, value);
                    });

        // Parser for compounding modifier
        public static readonly Parser<Modifier> compoundingModifier =
            ParserCombinators.String("!!")
                .Then(comparisonOperator.Optional())
                .SelectMany(
                    cOp => ParserCombinators.Integer,
                    (op, value) =>
                    {
                        return (Modifier)new CompoundingModifier(op.HasValue ? op.Value : ComparisonOperator.Equal, value);
                    });

        // Parser for reroll modifier
        public static readonly Parser<Modifier> rerollModifier =
            ParserCombinators.Char('r')
                .Then(ParserCombinators.Char('o').Optional())
                .SelectMany(_ => comparisonOperator, (ro, op) => (ro, op))
                .SelectMany(
                    _ => ParserCombinators.Integer,
                    (tup, value) =>
                    {
                        var (onlyOnce, cOp) = tup;
                        return (Modifier)new RerollModifier(cOp, value, onlyOnce.HasValue);
                    });

        // Parser for keep modifier
        public static readonly Parser<Modifier> keepModifier =
            ParserCombinators.String("kh")
                .SelectMany(
                    _ => ParserCombinators.OptionalInteger,
                    (_, count) =>
                    {
                        return (Modifier)new KeepModifier(count.HasValue ? count.Value : 1, true);
                    })
            .Or(
                ParserCombinators.String("kl")
                .SelectMany(
                    _ => ParserCombinators.OptionalInteger,
                    (_, count) =>
                    {
                        return (Modifier)new KeepModifier(count.HasValue ? count.Value : 1, false);
                    }));

        // Parser for drop modifier
        public static readonly Parser<Modifier> dropModifier =
            ParserCombinators.String("dh")
                .SelectMany(
                    _ => ParserCombinators.OptionalInteger,
                    (_, count) =>
                    {
                        return (Modifier)new DropModifier(count.HasValue ? count.Value : 1, false);
                    })
            .Or(
                ParserCombinators.String("dl")
                .SelectMany(
                    _ => ParserCombinators.OptionalInteger,
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
            ParserCombinators.Integer.Select(x => (Roll)new Constant(x));

        // Parser for a die type section
        public static readonly Parser<(int DieType, bool IsFudgeDie, bool IsPercentile)> dieTypeSection =
            ParserCombinators.Char('F')
                .ThenReturn((DieType: 0, IsFudgeDie: true, IsPercentile: false))
            .Or(ParserCombinators.Char('%')
                .ThenReturn((DieType: 0, IsFudgeDie: false, IsPercentile: true)))
            .Or(ParserCombinators.Integer.Select(dt => (DieType: dt, IsFudgeDie: false, IsPercentile: false)));

        // Parser for a basic roll
        public static readonly Parser<Roll> basicRoll =
            ParserCombinators.OptionalInteger
                .ThenSkip(ParserCombinators.Char('d'))
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
    ParserCombinators.Char('{')
        .Then(RollGroup.ThenSkip(ParserCombinators.Optional(ParserCombinators.Char(','))).Many())
        .ThenSkip(ParserCombinators.Char('}'))
        .SelectMany(
            rolls => Modifiers,
            (rolls, modifiers) => (Roll)new GroupedRoll(rolls, modifiers));

        // Top-level parser for any roll
        public static readonly Parser<Roll> Roll = groupedRoll.Or(basicRoll);
    }
}
