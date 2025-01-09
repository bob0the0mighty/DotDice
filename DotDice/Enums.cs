namespace DotDice;

using System.Collections.Generic;
using System.Linq;

// Enums for ComparisonOperator and SortDirection
public enum ComparisonOperator
{
    Equal,
    GreaterThan,
    LessThan
}

public enum SortDirection
{
    Ascending,
    Descending
}

public class BasicRoll : Roll
{
    public int NumberOfDice { get; set; }
    public int DieType { get; set; }
    public bool IsFudgeDie { get; set; }
    public bool IsPercentile { get; set; }
    public required List<Modifier> Modifiers { get; set; }
    public override string ToString()
    {
        return $"BasicRoll: NumberOfDice={NumberOfDice}, DieType={DieType}, IsFudgeDie={IsFudgeDie}, IsPercentile={IsPercentile}, Modifiers=[{string.Join(", ", Modifiers.Select(m => m.ToString()))}]";
    }
}

public class GroupedRoll : Roll
{
    public required List<Roll> Rolls { get; set; }
    public required List<GroupModifier> GroupModifiers { get; set; }
    public override string ToString()
    {
        return $"GroupedRoll: Rolls=[{string.Join(", ", Rolls.Select(r => r.ToString()))}], GroupModifiers=[{string.Join(", ", GroupModifiers.Select(gm => gm.ToString()))}]";
    }
}

public class Constant : Roll
{
    public int Value { get; set; }
    public override string ToString()
    {
        return $"Constant: Value={Value}";
    }
}

public abstract class Modifier { }

public class KeepModifier : Modifier
{
    public int Count { get; set; }
    public bool KeepHighest { get; set; } // true for kh, false for k
    public override string ToString()
    {
        return $"KeepModifier: Count={Count}, KeepHighest={KeepHighest}";
    }
}

public class DropModifier : Modifier
{
    public int Count { get; set; }
    public bool DropLowest { get; set; } // true for dl, false for d
    public override string ToString()
    {
        return $"DropModifier: Count={Count}, DropLowest={DropLowest}";
    }
}

public class RerollModifier : Modifier
{
    public ComparisonOperator Operator { get; set; }
    public int Value { get; set; }
    public bool OnlyOnce { get; set; }
    public override string ToString()
    {
        return $"RerollModifier: Operator={Operator}, Value={Value}, OnlyOnce={OnlyOnce}";
    }
}

public class ExplodeModifier : Modifier
{
    public ComparisonOperator Operator { get; set; }
    public int Value { get; set; }
    public override string ToString()
    {
        return $"ExplodeModifier: Operator={Operator}, Value={Value}";
    }
}

public class SuccessModifier : Modifier
{
    public ComparisonOperator Operator { get; set; }
    public int Value { get; set; }
    public override string ToString()
    {
        return $"SuccessModifier: Operator={Operator}, Value={Value}";
    }
}
public class FailureModifier : Modifier
{
    public ComparisonOperator Operator { get; set; }
    public int Value { get; set; }
    public override string ToString()
    {
        return $"FailureModifier: Operator={Operator}, Value={Value}";
    }
}

public class SortModifier : Modifier
{
    public SortDirection Direction { get; set; }
    public override string ToString()
    {
        return $"SortModifier: Direction={Direction}";
    }
}

public abstract class GroupModifier : Modifier { }