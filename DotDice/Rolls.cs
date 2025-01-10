namespace DotDice
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;

    public abstract record Roll
    {
    }

    public record BasicRoll(
        int NumberOfDice,
        int DieType,
        bool IsFudgeDie,
        bool IsPercentile,
        List < Modifier > Modifiers
    ): Roll
    {
        public override string ToString()
        {
            return $"BasicRoll: NumberOfDice={NumberOfDice}, DieType={DieType}, IsFudgeDie={IsFudgeDie}, IsPercentile={IsPercentile}, Modifiers=[{string.Join(", ", Modifiers.Select(m => m.ToString()))}]";
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
}