using System.Diagnostics.Tracing;

namespace DotDice;

public static class ParserCombinators
{
    // Basic combinators

    // Return a parser that always succeeds with the given value
    public static Parser<T> Return<T>(T value)
    {
        return (input, position) => ParserResult<T>.Succeed(value, position);
    }

    // Run the parser and return the result if it succeeds
    public static ParserResult<T> Run<T>(this Parser<T> parser, string input)
    {
        return parser(input, 0);
    }

    // Choice combinator: try the first parser, if it fails, try the second
    public static Parser<T> Or<T>(this Parser<T> p1, Parser<T> p2)
    {
        return (input, position) =>
        {
            var result1 = p1(input, position);
            if (result1.Success)
            {
                return result1;
            }
            else
            {
                return p2(input, position);
            }
        };
    }

    // Apply a function to the result of a parser
    public static Parser<U> Select<T, U>(this Parser<T> parser, Func<T, U> selector)
    {
        return (input, position) =>
        {
            var result = parser(input, position);
            if (result.Success)
            {
                return ParserResult<U>.Succeed(selector(result.Value), result.Position);
            }
            else
            {
                return ParserResult<U>.Fail(result.Error, result.Position);
            }
        };
    }

    // Sequence two parsers and combine their results with a function
    public static Parser<V> SelectMany<T, U, V>(
        this Parser<T> parser,
        Func<T, Parser<U>> selector,
        Func<T, U, V> projector)
    {
        return (input, position) =>
        {
            var result1 = parser(input, position);
            if (!result1.Success)
            {
                return ParserResult<V>.Fail(result1.Error, result1.Position);
            }

            var result2 = selector(result1.Value)(input, result1.Position);
            if (!result2.Success)
            {
                return ParserResult<V>.Fail(result2.Error, result2.Position);
            }

            return ParserResult<V>.Succeed(projector(result1.Value, result2.Value), result2.Position);
        };
    }

    // Other helpful combinators

    // Parse a single character satisfying a predicate
    public static Parser<char> Satisfy(Predicate<char> predicate)
    {
        return (input, position) =>
        {
            if (position >= input.Length)
            {
                return ParserResult<char>.Fail("Unexpected end of input", position);
            }

            char c = input[position];
            if (predicate(c))
            {
                return ParserResult<char>.Succeed(c, position + 1);
            }
            else
            {
                return ParserResult<char>.Fail($"Unexpected character: '{c}'", position);
            }
        };
    }

    // Parse a specific character
    public static Parser<char> Char(char c)
    {
        return Satisfy(ch => ch == c);
    }

    // Parse any character in a string
    public static Parser<char> AnyOf(string chars)
    {
        return Satisfy(chars.Contains);
    }

    // Parse one or more occurrences of a parser
    public static Parser<List<T>> OneOrMore<T>(this Parser<T> parser)
    {
        return parser.SelectMany(
            x => parser.Many().Select(xs => new List<T> { x }.Concat(xs).ToList()),
            (x, xs) => xs);
    }

    // Parse zero or more occurrences of a parser
    public static Parser<List<T>> Many<T>(this Parser<T> parser)
    {
        return parser.OneOrMore().Or(Return(new List<T>()));
    }

    // Parse a string
    public static Parser<string> String(string str)
    {
        return str.Select(Char)
            .Aggregate((p1, p2) => p1.Then(p2))
            .Select(c => c.ToString());
    }

    // Optionally parse something
    public static Parser<Option<T>> Optional<T>(this Parser<T> parser)
    {
        return parser.Select(x => new Option<T>(x))
            .Or(Return(Option<T>.None()));
    }

    // A parser for whitespace
    public static readonly Parser<string> Whitespace =
        Satisfy(char.IsWhiteSpace)
            .Many()
            .Select(x => new string(x.ToArray()));

    // Skip whitespace before and after a parser
    public static Parser<T> Token<T>(this Parser<T> parser)
    {
        return Whitespace.Then(parser).ThenSkip(Whitespace);
    }

    // Sequence two parsers and keep the result of the first
    public static Parser<T> ThenSkip<T, U>(this Parser<T> parser, Parser<U> other)
    {
        return parser.SelectMany(_ => other, (x, _) => x);
    }

    // Sequence two parsers and keep the result of the second
    public static Parser<U> Then<T, U>(this Parser<T> parser, Parser<U> other)
    {
        return parser.SelectMany(_ => other, (_, y) => y);
    }

    // A parser for an integer with an optional negative sign
    public static readonly Parser<int> Integer =
        Char('-')
            .Optional()
            .Then(Satisfy(char.IsDigit).OneOrMore())
            .Select(
                result =>
                {
                    Option<char> sign = Option<char>.None();
                    List<char> digits;
                    if(result.First() == '-'){
                        sign = new('-');
                        digits = result.Skip(1)
                            .ToList();
                    } else {
                        digits = result;
                    }

                    var value = int.Parse(new string(digits.ToArray()));
                    return sign.HasValue ? -value : value;
                });

    // A parser for an optional integer
    public static readonly Parser<Option<int>> OptionalInteger = Integer.Optional();
}
