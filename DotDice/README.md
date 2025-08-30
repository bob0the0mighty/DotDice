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
- Extensible architecture for adding custom dice types and modifiers

## Installation

Install DotDice via NuGet:

```bash
dotnet add package DotDice
```

## Usage

### Basic Dice Rolling

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

DotDice currently supports the following dice types:

- `d#` - Standard dice (d4, d6, d8, d10, d12, d20, etc.)
- `d%` or `d100` - Percentile dice
- `dF` - Fudge/Fate dice

And the following modifiers:

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

## Contributing

Contributions are welcome! Feel free to submit issues or pull requests on GitHub.

## License

DotDice is licensed under the MIT license. See the [LICENSE](LICENSE) file for details.
