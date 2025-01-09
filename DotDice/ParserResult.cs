namespace DotDice;

using System;
using System.Collections.Generic;
using System.Linq;

// Define a delegate for parsers
public delegate ParserResult<T> Parser<T>(string input, int position);

// Define the result of a parser
public class ParserResult<T>
{
    public bool Success { get; }
    public T Value { get; }
    public string Error { get; }
    public int Position { get; }

    private ParserResult(bool success, T value, string error, int position)
    {
        Success = success;
        Value = value;
        Error = error;
        Position = position;
    }

    public static ParserResult<T> Succeed(T value, int position)
    {
        return new ParserResult<T>(true, value, null, position);
    }

    public static ParserResult<T> Fail(string error, int position)
    {
        return new ParserResult<T>(false, default(T), error, position);
    }
}


// Option type similar to F#'s Option
public struct Option<T>
{
    public bool HasValue { get; }
    public T Value { get; }

    public Option(T value)
    {
        HasValue = true;
        Value = value;
    }

    public static Option<T> None()
    {
        return new Option<T>();
    }

    public U Match<U>(Func<T, U> some, Func<U> none)
    {
        return HasValue ? some(Value) : none();
    }

    public void Match(Action<T> some, Action none)
    {
        if (HasValue)
        {
            some(Value);
        }
        else
        {
            none();
        }
    }
}

// AST definitions (same as before)
public abstract class Roll { }

// ... (rest of AST definitions)

// Parser functions using combinators
public static class DiceParserCombinators
{
    // Parser for comparison operators
    private static readonly Parser<ComparisonOperator> _ComparisonOperator =
        ParserCombinators.Char('=')
            .ThenReturn(ComparisonOperator.Equal)
        .Or(ParserCombinators.Char('>')
            .ThenReturn(ComparisonOperator.GreaterThan))
        .Or(ParserCombinators.Char('<')
            .ThenReturn(ComparisonOperator.LessThan));

    // Parser for sort directions
    private static readonly Parser<SortDirection> _SortDirection =
        ParserCombinators.Char('a')
            .ThenReturn(SortDirection.Ascending)
        .Or(ParserCombinators.Char('d')
            .ThenReturn(SortDirection.Descending));

    // Parser for sort modifier
    private static readonly Parser<Modifier> _SortModifier =
        ParserCombinators.Char('s')
            .SelectMany(
                sort => _SortDirection,
                (sort, value) => (Modifier)new SortModifier { Direction = value }
            );

    // Parser for success modifier
    private static readonly Parser<Modifier> _SuccessModifier =
        _ComparisonOperator
            .SelectMany(
                cOp => ParserCombinators.Integer,
                (cOp, value) => (Modifier)new SuccessModifier { Operator = cOp, Value = value }
            );

    // Parser for failure modifier
    private static readonly Parser<Modifier> _FailureModifier =
        ParserCombinators.Char('f')
            .Then(_ComparisonOperator)
            .SelectMany(
                parser => ParserCombinators.Integer,
                (cOp, value) => (Modifier)new FailureModifier { Operator = cOp, Value = value }
            );

    // Parser for explode modifier
    private static readonly Parser<Modifier> _ExplodeModifier =
        ParserCombinators.Char('!')
            .Then(_ComparisonOperator)
            .Then(ParserCombinators.Integer)
            .Select(x => (Modifier)new ExplodeModifier { Operator = x.Item1, Value = x.Item2 });

    // Parser for reroll modifier
    private static readonly Parser<Modifier> _RerollModifier =
        ParserCombinators.Char('r')
            .Then(ParserCombinators.Char('o').Optional())
            .Then(_ComparisonOperator)
            .Then(ParserCombinators.Integer)
            .Select(
                x =>
                {
                    var ((((r, onlyOnce), op), value) = x;
                    return (Modifier)new RerollModifier { Operator = op, Value = value, OnlyOnce = onlyOnce.HasValue };
                });

    // Parser for keep modifier
    private static readonly Parser<Modifier> _KeepModifier =
        ParserCombinators.Char('k')
            .Then(ParserCombinators.Char('h').Optional())
            .Then(ParserCombinators.Char('l').Optional())
            .Then(ParserCombinators.OptionalInteger)
            .Select(
                x =>
                {
                    var (((k, h), l), count) = x;
                    return (Modifier)new KeepModifier { Count = count.HasValue ? count.Value : 1, KeepHighest = h.HasValue };
                });

    // Parser for drop modifier
    private static readonly Parser<Modifier> _DropModifier =
        ParserCombinators.Char('d')
            .Then(ParserCombinators.Char('l').Optional())
            .Then(ParserCombinators.Char('h').Optional())
            .Then(ParserCombinators.OptionalInteger)
            .Select(
                x =>
                {
                    var (((d, l), h), count) = x;
                    return (Modifier)new DropModifier { Count = count.HasValue ? count.Value : 1, DropLowest = l.HasValue };
                });

    // Parser for a single modifier
    private static readonly Parser<Modifier> Modifier =
        _KeepModifier
            .Or(_DropModifier)
            .Or(_RerollModifier)
            .Or(_ExplodeModifier)
            .Or(_SuccessModifier)
            .Or(_FailureModifier)
            .Or(_SortModifier);

    // Parser for multiple modifiers
    private static readonly Parser<List<Modifier>> Modifiers = Modifier.Many();

    // Parser for group modifiers - limited to success, failure, and sort for simplicity
    private static readonly Parser<GroupModifier> GroupModifier =
        _SuccessModifier.Select(x => (GroupModifier)x)
            .Or(_FailureModifier.Select(x => (GroupModifier)x))
            .Or(_SortModifier.Select(x => (GroupModifier)x));

    private static readonly Parser<List<GroupModifier>> GroupModifiers = GroupModifier.Many();

    // Parser for a constant
    private static readonly Parser<Roll> Constant =
        ParserCombinators.Integer.Select(x => (Roll)new Constant { Value = x });

    // Parser for a basic roll
    private static readonly Parser<Roll> BasicRoll =
        ParserCombinators.OptionalInteger
            .ThenSkip(ParserCombinators.Char('d'))
            .Then(
                ParserCombinators.Char('F').ThenReturn((dieType: 0, isFudge: true, isPercentile: false))
                .Or(ParserCombinators.Char('%').ThenReturn((dieType: 0, isFudge: false, isPercentile: true)))
                .Or(ParserCombinators.Integer.Select(dt => (dieType: dt, isFudge: false, isPercentile: false)))
            )
            .Then(Modifiers)
            .Select(
                x =>
                {
                    var ((numDice, (dieType, isFudge, isPercentile)), modifiers) = x;
                    return (Roll)new BasicRoll
                    {
                        NumberOfDice = numDice.HasValue ? numDice.Value : 1,
                        DieType = dieType,
                        IsFudgeDie = isFudge,
                        IsPercentile = isPercentile,
                        Modifiers = modifiers
                    };
                });

    // Parser for a roll group (either a basic roll or a constant)
    private static readonly Parser<Roll> RollGroup = BasicRoll.Or(Constant);

    // Parser for a grouped roll
    private static readonly Parser<Roll> GroupedRoll =
        ParserCombinators.Char('{')
            .Then(RollGroup.ThenSkip(ParserCombinators.Optional(ParserCombinators.Char(','))).Many())
            .ThenSkip(ParserCombinators.Char('}'))
            .Then(GroupModifiers)
            .Select(x => (Roll)new GroupedRoll { Rolls = x.Item1, GroupModifiers = x.Item2 });

    // Top-level parser for any roll
    public static readonly Parser<Roll> Roll = GroupedRoll.Or(BasicRoll);
}

// Extension method for easily running a parser and returning a specific result
public static class ParserExtensions
{
    public static Parser<U> ThenReturn<T, U>(this Parser<T> parser, U value)
    {
        return parser.Select(_ => value); // Assuming the parser doesn't need input for this case
    }
}