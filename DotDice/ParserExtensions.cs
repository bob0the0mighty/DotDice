namespace DotDice
{
    // Extension method for easily running a parser and returning a specific result
    public static class ParserExtensions
    {
        public static Parser<U> ThenReturn<T, U>(this Parser<T> parser, U value)
        {
            return parser.Select(_ => value); // Assuming the parser doesn't need input for this case
        }
    }
}