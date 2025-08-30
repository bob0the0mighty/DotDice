namespace DotDice.Parser
{

    public abstract record Roll
    {
    }

    public record BasicRoll(
        int NumberOfDice,
        DieType DieType,
        IEnumerable<Modifier> Modifiers
    ) : Roll
    {
        public override string ToString()
        {
            return $"BasicRoll: NumberOfDice={NumberOfDice}, DieType={DieType}, Modifiers=[{string.Join(", ", Modifiers.Select(m => m.ToString()))}]";
        }
    }

    public record GroupedRoll(
        List<Roll> Rolls,
        List<Modifier> Modifiers
    ) : Roll
    {
        public override string ToString()
        {
            return $"GroupedRoll: Rolls=[{string.Join(", ", Rolls.Select(r => r.ToString()))}], Modifiers=[{string.Join(", ", Modifiers.Select(m => m.ToString()))}]";
        }
    }

    public record Constant(int Value) : Roll
    {
        public override string ToString()
        {
            return $"Constant: Value={Value}";
        }
    }

    public record ArithmeticRoll(
        List<(ArithmeticOperator Operator, Roll Roll)> Terms
    ) : Roll
    {
        public override string ToString()
        {
            return $"ArithmeticRoll: Terms=[{string.Join(", ", Terms.Select(t => $"{t.Operator} {t.Roll}"))}]";
        }
    }
}