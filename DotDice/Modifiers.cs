namespace DotDice
{
    static class Constants{
        public const string HIGHEST = "highest";
        public const string LOWEST = "lowest";
    }

    public abstract record Modifier
    {
        public override string ToString()
        {
            return this.GetType().Name;
        }
    }

    public record KeepModifier(int Count, bool KeepHighest) : Modifier
    {
        public override string ToString()
        {
            return $"Keep {(KeepHighest ? Constants.HIGHEST : Constants.LOWEST)} {Count} dice";
        }
    }

    public record DropModifier(int Count, bool DropHighest) : Modifier
    {
        public override string ToString()
        {
            return $"Drop {(DropHighest ? Constants.HIGHEST : Constants.LOWEST)} {Count}";
        }
    }

    public record RerollOnceModifier(ComparisonOperator Operator, int Value) : Modifier
    {
        public override string ToString()
        {
            return $"Reroll dice {Operator} than {Value} once";
        }
    }

    public record RerollUntilModifier(ComparisonOperator Operator, int Value) : Modifier
    {
        public override string ToString()
        {
            return $"Reroll until result is {Operator} than {Value}";
        }
    }

    public record ExplodeModifier(ComparisonOperator Operator, int Value) : Modifier
    {
        public override string ToString()
        {
            return $"Explode dice {Operator} than {Value}";
        }
    }

    public record CompoundingModifier(ComparisonOperator Operator, int Value) : Modifier
    {
        public override string ToString()
        {
            return $"CompoundingModifier: Operator={Operator}, Value={Value}";
        }
    }

    public record SuccessModifier(ComparisonOperator Operator, int Value) : Modifier
    {
        public override string ToString()
        {
            return $"SuccessModifier: Operator={Operator}, Value={Value}";
        }
    }
    public record FailureModifier(ComparisonOperator Operator, int Value) : Modifier
    {
        public override string ToString()
        {
            return $"Fail rolls {Operator} than {Value}";
        }
    }

    public record ConstantModifier(ArithmaticOperator Operator, int Value) : Modifier
    {
        public override string ToString()
        {
            return $"{Operator} {Value} to final roll";
        }
    }
}