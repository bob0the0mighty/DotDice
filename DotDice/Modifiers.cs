namespace DotDice
{
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
            return $"KeepModifier: Count={Count}, KeepHighest={KeepHighest}";
        }
    }

    public record DropModifier(int Count, bool DropHighest) : Modifier
    {
        public override string ToString()
        {
            return $"DropModifier: Count={Count}, DropLowest={DropHighest}";
        }
    }

    public record RerollOnceModifier(ComparisonOperator Operator, int Value) : Modifier
    {
        public override string ToString()
        {
            return $"RerollOnceModifier: Operator={Operator}, Value={Value}";
        }
    }

    public record RerollCompoundModifier(ComparisonOperator Operator, int Value) : Modifier
    {
        public override string ToString()
        {
            return $"RerollCompoundModifier: Operator={Operator}, Value={Value}";
        }
    }

    public record ExplodeModifier(ComparisonOperator Operator, int Value) : Modifier
    {
        public override string ToString()
        {
            return $"ExplodeModifier: Operator={Operator}, Value={Value}";
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
            return $"FailureModifier: Operator={Operator}, Value={Value}";
        }
    }

    public record SortModifier(SortDirection Direction) : Modifier
    {
        public override string ToString()
        {
            return $"SortModifier: Direction={Direction}";
        }
    }
}