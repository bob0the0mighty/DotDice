namespace DotDice
{
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
}
