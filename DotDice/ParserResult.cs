namespace DotDice
{
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
            return new ParserResult<T>(true, value, string.Empty, position);
        }

        public static ParserResult<T> Fail(string error, int position)
        {
            return new ParserResult<T>(false, default, error, position);
        }
    }
}