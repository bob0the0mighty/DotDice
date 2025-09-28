# DotDice

A flexible and extensible dice rolling library for .NET applications, designed to support a wide variety of tabletop role-playing games (RPGs) and board games.

## Features

- Support for multiple dice types:
  - Standard dice (d4, d6, d8, d10, d12, d20, etc.)
  - Percentile dice (d100)
  - Fudge/Fate dice (-1, 0, +1)
- Comprehensive set of dice modifiers:
  - Keep highest/lowest dice
  - Drop highest/lowest dice
  - Reroll dice (once or multiple times)
  - Exploding dice
  - Compounding dice
  - Success counting
  - Failure counting
  - Constant modifiers (+/-X)
- Support for complex dice mechanics from various tabletop games:
  - D&D (advantage/disadvantage, ability checks, critical hits)
  - Savage Worlds (wild die, exploding dice)
  - World of Darkness/Storyteller (success counting)
  - Shadowrun (success counting)
  - FATE/Fudge (fudge dice)
  - Call of Cthulhu (percentile tests)
- Arithmetic roll expressions (e.g., `3d20+5d6-1d4+1`)
- Detailed evaluation results with individual die events and structural grouping information
- Extensible architecture for adding custom dice types and modifiers

## Installation

Install DotDice via NuGet:

```bash
dotnet add package DotDice
```

## Usage

### Simple String-Based API

The easiest way to use DotDice is through the string extension methods:

```csharp
using DotDice.Extension;

// Basic dice rolls
int result1 = "1d6".ParseRoll();        // Roll a six-sided die
int result2 = "3d6".ParseRoll();        // Roll three six-sided dice
int result3 = "1d20+5".ParseRoll();     // Roll d20 with +5 modifier

Console.WriteLine($"1d6: {result1}");
Console.WriteLine($"3d6: {result2}");
Console.WriteLine($"1d20+5: {result3}");
```

### Arithmetic Roll Expressions

DotDice supports complex arithmetic expressions combining multiple dice rolls and constants:

```csharp
// Multiple dice types in one expression
int damage = "2d6+1d4+3".ParseRoll();           // Sword + dagger + strength modifier
int healing = "3d4+2d6".ParseRoll();            // Healing potion + spell bonus

// Complex expressions like those found in RPG systems
int abilityCheck = "1d20+1d4-2".ParseRoll();    // D&D ability check with guidance and penalty
int shadowrunTest = "5d6>4+3d6>4".ParseRoll();  // Shadowrun: attribute + skill dice

Console.WriteLine($"Damage: {damage}");
Console.WriteLine($"Ability Check: {abilityCheck}");
```

### Detailed Evaluation Results

For applications that need to know what happened during the roll (individual die results, which dice were dropped, etc.), use the detailed evaluation API:

```csharp
using DotDice.Extension;
using DotDice.Evaluator;

// Get detailed results
var detailedResult = "2d6kh1".ParseRollDetailed();  // Roll 2d6, keep highest

Console.WriteLine($"Final Value: {detailedResult.Value}");
Console.WriteLine("Individual Events:");

foreach (var evt in detailedResult.Events)
{
    Console.WriteLine($"  Die: {evt.DieType}, Value: {evt.Value}, Status: {evt.Status}");
    // evt.Type shows if it was Initial, Reroll, Explode, etc.
    // evt.Significance shows if it was a critical hit, minimum roll, etc.
}
```

#### Arithmetic Expression Grouping

For complex arithmetic expressions like "3d20-4d4+5", the detailed results now include structural information about which dice belonged to which group and what operations separated them:

```csharp
var result = "3d20kh1-4d4+5".ParseRollDetailed();

// Group events by their roll group
var groups = result.Events.GroupBy(e => e.GroupId).ToList();

// Handle grouped events (arithmetic expressions)
var groupedEvents = groups.Where(g => g.Key != null).ToList();
foreach (var group in groupedEvents)
{
    var op = group.First().GroupOperator;
    var keptDice = group.Where(e => e.Status == DieStatus.Kept).ToList();
    var droppedDice = group.Where(e => e.Status == DieStatus.Dropped).ToList();
    
    Console.WriteLine($"Group {group.Key} ({op}): Total = {keptDice.Sum(e => e.Value)}");
    
    if (keptDice.Any())
        Console.WriteLine($"  Kept: {string.Join(", ", keptDice.Select(e => e.Value))}");
        
    if (droppedDice.Any()) 
        Console.WriteLine($"  Dropped: {string.Join(", ", droppedDice.Select(e => e.Value))}");
}

// Handle single roll events (no grouping information)
var singleRollEvents = groups.Where(g => g.Key == null).SelectMany(g => g).ToList();
if (singleRollEvents.Any())
{
    Console.WriteLine("Single Roll Events (no grouping):");
    foreach (var evt in singleRollEvents)
    {
        Console.WriteLine($"  Die: {evt.DieType}, Value: {evt.Value}, Status: {evt.Status}");
    }
}

// Output example:
// Group 0 (Add): Total = 18
//   Kept: 18
//   Dropped: 5, 12
// Group 1 (Subtract): Total = 14  
//   Kept: 3, 4, 3, 4
// Group 2 (Add): Total = 5
//   Kept: 5
```

**Key Properties for Grouping:**
- `GroupId`: Unique identifier for each roll group (0, 1, 2, etc.)
- `GroupOperator`: The arithmetic operator for this group (`Add` or `Subtract`)
- Single rolls (like "2d6") have `null` for both properties to maintain backward compatibility

This makes it easy to:
- Reconstruct the original expression structure
- Show which dice were affected by modifiers in each group
- Display results in a user-friendly grouped format
- Implement custom logic based on roll groups

### Advanced API (Direct Object Creation)

For more complex scenarios or when you need fine-grained control, you can create roll objects directly:

```csharp
using DotDice.Evaluator;
using DotDice.Parser;

// Create an evaluator
var evaluator = new DiceEvaluator();

// Roll 3d6 (three six-sided dice)
var basicRoll = new BasicRoll(3, new DieType.Basic(6), new List<Modifier>());
int result = evaluator.Evaluate(basicRoll);
Console.WriteLine($"3d6: {result}");

// Roll 1d20 with +5 modifier
var attackRoll = new BasicRoll(1, new DieType.Basic(20), 
                               new List<Modifier> { new ConstantModifier(ArithmeticOperator.Add, 5) });
int attackResult = evaluator.Evaluate(attackRoll);
Console.WriteLine($"1d20+5: {attackResult}");
```

### Using Modifiers

```csharp
// Roll 4d6, drop the lowest die (common for D&D ability scores)
var abilityRoll = new BasicRoll(4, new DieType.Basic(6),
                                new List<Modifier> { new DropModifier(1, false) });
int abilityScore = evaluator.Evaluate(abilityRoll);
Console.WriteLine($"4d6 drop lowest: {abilityScore}");

// Roll with advantage (2d20 keep highest)
var advantageRoll = new BasicRoll(2, new DieType.Basic(20),
                                 new List<Modifier> { new KeepModifier(1, true) });
int advantageResult = evaluator.Evaluate(advantageRoll);
Console.WriteLine($"Advantage (2d20kh1): {advantageResult}");

// Exploding dice (d6, explode on 6)
var explodingRoll = new BasicRoll(1, new DieType.Basic(6),
                                 new List<Modifier> { new ExplodeModifier(ComparisonOperator.Equal, 6) });
int explodeResult = evaluator.Evaluate(explodingRoll);
Console.WriteLine($"1d6 exploding: {explodeResult}");
```

### Success-Based Systems

```csharp
// Shadowrun style: roll 5d6, count successes (5 or 6)
var shadowrunRoll = new BasicRoll(5, new DieType.Basic(6),
                                 new List<Modifier> { new SuccessModifier(ComparisonOperator.GreaterThan, 4) });
int successes = evaluator.Evaluate(shadowrunRoll);
Console.WriteLine($"Shadowrun (5d6, threshold 5): {successes} successes");

// World of Darkness: roll 4d10, 8+ success, 10s explode
var wodRoll = new BasicRoll(4, new DieType.Basic(10), new List<Modifier> { 
                            new ExplodeModifier(ComparisonOperator.Equal, 10),
                            new SuccessModifier(ComparisonOperator.GreaterThan, 7) });
int wodSuccesses = evaluator.Evaluate(wodRoll);
Console.WriteLine($"World of Darkness: {wodSuccesses} successes");
```

### Advanced Multi-Roll Scenarios

More complex game systems may require multiple rolls that interact with each other. You can use the results of multiple evaluations:

```csharp
// Savage Worlds trait test with wild die
var traitRoll = new BasicRoll(1, new DieType.Basic(8), 
                             new List<Modifier> { new ExplodeModifier(ComparisonOperator.Equal, 8) });
var wildDieRoll = new BasicRoll(1, new DieType.Basic(6), 
                               new List<Modifier> { new ExplodeModifier(ComparisonOperator.Equal, 6) });

int traitResult = evaluator.Evaluate(traitRoll);
int wildDieResult = evaluator.Evaluate(wildDieRoll);
int finalResult = Math.Max(traitResult, wildDieResult);

Console.WriteLine($"Savage Worlds: Trait d8 = {traitResult}, Wild Die d6 = {wildDieResult}, Final = {finalResult}");
```

## Supported Dice Notations

DotDice supports a comprehensive dice notation syntax:

### Basic Dice
- `d#` - Standard dice (d4, d6, d8, d10, d12, d20, etc.)
- `d%` or `d100` - Percentile dice
- `dF` - Fudge/Fate dice

### Arithmetic Expressions
- `+` - Addition (e.g., `1d6+3`, `2d6+1d4`)
- `-` - Subtraction (e.g., `1d20-2`, `3d6-1d4`)
- Complex expressions: `3d20+5d6-1d4+1`

### Modifiers
- `kh#` - Keep highest # dice
- `kl#` - Keep lowest # dice
- `dh#` - Drop highest # dice
- `dl#` - Drop lowest # dice
- `r<#`, `r>#`, `r=#` - Reroll once if less than, greater than, or equal to #
- `rr<#`, `rr>#`, `rr=#` - Reroll multiple times until condition is no longer met
- `!=#`, `!>#`, `!<#` - Explode if condition is met
- `^=#`, `^>#`, `^<#` - Compound if condition is met
- `+#`, `-#` - Add or subtract a constant value
- `cs>#`, `cs<#`, `cs=#` - Count successes (greater than, less than, or equal to #)
- `cf>#`, `cf<#`, `cf=#` - Count failures (greater than, less than, or equal to #)

### Examples
- `4d6kh3` - Roll 4d6, keep highest 3 (D&D ability scores)
- `2d20kh1` - Roll 2d20, keep highest (D&D advantage)
- `1d6!=6` - Roll 1d6, explode on 6 (Savage Worlds)
- `5d6>4` - Roll 5d6, count successes of 5+ (Shadowrun)
- `3d6+2d4-1` - Roll 3d6 plus 2d4 minus 1

## Contributing

Contributions are welcome! Feel free to submit issues or pull requests on GitHub.

## License

DotDice is licensed under the MIT license. See the [LICENSE](LICENSE) file for details.