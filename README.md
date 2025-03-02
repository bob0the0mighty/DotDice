# DotDice

A C# library for parsing and evaluating dice rolls.

## Features

* Parses complex dice roll expressions, including:
    * Multiple dice (e.g., `3d6`)
    * Different die types (e.g., `d6`, `d20`, `dF`, `d%`)
    * Modifiers (e.g., `kh2`, `dl1`, `!>10`, `ro<3`)
    * Constant modifiers (e.g., `+5`, `-2`)
* Evaluates parsed dice rolls to generate results.

## Usage

1.  Install the DotDice NuGet package.
2.  Use the `DiceParser` class to parse dice roll expressions:

```csharp
using DotDice;

string expression = "3d6 + 5";
var roll = DiceParser.Roll.ParseOrThrow(expression);