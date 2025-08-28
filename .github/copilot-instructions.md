# DotDice - .NET Dice Rolling Library

DotDice is a flexible and extensible dice rolling library for .NET 8.0 applications, designed to support tabletop role-playing games (RPGs) and board games. It provides comprehensive dice notation parsing and evaluation with support for complex modifiers.

**Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.**

## Working Effectively

### Bootstrap and Build the Repository
Run these commands in sequence to set up the development environment:

- `dotnet restore` -- restores NuGet packages. Takes ~7 seconds. NEVER CANCEL.
- `dotnet build` -- builds the solution. Takes ~8 seconds. NEVER CANCEL. Set timeout to 30+ seconds.
  - Produces warnings (CS8618 nullable, NUnit2005 assertion style) but builds successfully
  - These warnings are existing and not blockers
- `dotnet test` -- runs all 350 tests. Takes ~4-6 seconds. NEVER CANCEL. Set timeout to 30+ seconds.

### Code Quality and Formatting
- `dotnet format --verify-no-changes` -- checks formatting issues. Takes ~10 seconds. NEVER CANCEL. Set timeout to 30+ seconds.
  - Currently detects whitespace formatting issues in test files
- `dotnet format` -- fixes formatting issues. Takes ~10 seconds. NEVER CANCEL. Set timeout to 30+ seconds.
- **ALWAYS run `dotnet format` before committing changes** or the GitHub Actions CI will detect formatting violations.

### Testing and Coverage
- `dotnet test -c Release --collect:"XPlat Code Coverage"` -- runs tests with coverage collection. Takes ~6 seconds. NEVER CANCEL. Set timeout to 30+ seconds.
- The test suite includes 350 comprehensive tests covering:
  - Basic dice rolling (1d6, 3d6, 1d20+5)
  - Advanced modifiers (exploding dice, advantage/disadvantage, keep/drop)
  - Success counting systems (Shadowrun, World of Darkness)
  - Special dice types (Fudge, percentile)
  - Complex multi-roll scenarios

### Packaging
- `dotnet pack ./DotDice/DotDice.csproj -c Release -o . -p:Version=X.Y.Z` -- creates NuGet package. Takes ~2-3 seconds. NEVER CANCEL.

## Validation

### Manual Testing Requirements
**ALWAYS manually validate any code changes** by running through these complete scenarios after making modifications:

#### Basic Functionality Test
Create a simple console application and verify:
```csharp
using DotDice.Extension;

// Basic dice rolling
Console.WriteLine($"1d6: {"1d6".ParseRoll()}");
Console.WriteLine($"3d6: {"3d6".ParseRoll()}");
Console.WriteLine($"1d20+5: {"1d20+5".ParseRoll()}");

// Advanced modifiers
Console.WriteLine($"4d6kh3 (D&D ability): {"4d6kh3".ParseRoll()}");
Console.WriteLine($"2d20kh1 (advantage): {"2d20kh1".ParseRoll()}");
Console.WriteLine($"1d6!>5 (exploding): {"1d6!>5".ParseRoll()}");

// Success counting
Console.WriteLine($"5d6>4 (successes): {"5d6>4".ParseRoll()}");

// Special dice
Console.WriteLine($"4dF (Fudge): {"4dF".ParseRoll()}");
Console.WriteLine($"1d% (percentile): {"1d%".ParseRoll()}");
```

**CRITICAL**: All dice rolls should return reasonable integer values without exceptions. Verify output shows different random results on multiple runs.

#### End-to-End Workflow Validation
1. Build the solution successfully
2. Run all tests and verify 350 tests pass
3. Create test application using DotDice library
4. Execute multiple dice roll scenarios and verify randomness
5. Run formatting check and fix any issues
6. Package the library successfully

**NEVER skip validation** - if any step fails, investigate and fix before proceeding.

## Repository Structure

### Key Projects
- **DotDice/** -- Main library project (.NET 8.0)
  - `src/Evaluator/` -- Core dice evaluation engine
  - `src/Parser/` -- Dice notation parsing (uses Pidgin parser library)
  - `src/Extension/` -- String extension methods for easy usage
  - `src/RandomNumberGenerator/` -- Random number generation interfaces
- **DotDiceTests/** -- Test project (NUnit 3.14.0)
  - `src/Evaluator/` -- Tests for dice evaluation
  - `src/Parser/` -- Tests for notation parsing
  - `src/Extension/` -- Tests for string extensions
- **dotDice.sln** -- Solution file
- **grammar** -- Grammar specification for dice notation syntax

### Important Files
- `README.md` -- Main documentation with usage examples
- `DotDice/README.md` -- Package-specific documentation
- `CHANGELOG.md` -- Version history
- `grammar` -- Formal grammar definition for dice notation
- `.github/workflows/` -- CI/CD pipelines for build/test and NuGet publishing
- `codecov.yml` -- Code coverage configuration (80% target)

### Common Commands Reference
The following are outputs from frequently run commands. Reference them instead of running bash commands to save time:

#### Repository Structure
```
/home/runner/work/DotDice/DotDice/
├── .github/workflows/          # CI/CD pipelines
├── DotDice/                    # Main library project
│   ├── src/                    # Source code
│   └── DotDice.csproj         # Project file (.NET 8.0)
├── DotDiceTests/               # Test project
│   ├── src/                    # Test source code
│   └── DotDiceTests.csproj    # Test project file (NUnit)
├── dotDice.sln                # Visual Studio solution
├── README.md                  # Main documentation
├── CHANGELOG.md               # Version history
├── grammar                    # Dice notation grammar
└── codecov.yml               # Coverage configuration
```

#### Key Dependencies (from DotDice.csproj)
- **Pidgin 3.3.0** -- Parser combinator library for dice notation parsing
- **Target Framework**: .NET 8.0
- **Test Framework**: NUnit 3.14.0 with Microsoft.NET.Test.Sdk 17.8.0

## Advanced Usage Patterns

### String Extension API (Recommended)
```csharp
using DotDice.Extension;

int result = "1d20+5".ParseRoll();
```

### Direct API Usage
```csharp
using DotDice.Evaluator;
using DotDice.Parser;

var evaluator = new DiceEvaluator();
var basicRoll = new BasicRoll(3, new DieType.Basic(6), new List<Modifier>());
int result = evaluator.Evaluate(basicRoll);
```

### Supported Dice Notations
- Basic: `1d6`, `3d8`, `2d10+5`, `1d20-3`
- Keep/Drop: `4d6kh3`, `5d8kl2`, `6d6dh1`, `4d10dl1`
- Exploding: `1d6!>5`, `3d10!=10`
- Advantage/Disadvantage: `2d20kh1`, `2d20kl1`
- Success Counting: `5d6>4`, `4d10>7`
- Failure Counting: `6d6f<3`, `4d8f=1`
- Rerolling: `3d6ro=1`, `2d8rc<3`
- Special Dice: `4dF` (Fudge), `1d%` (percentile)
- Complex: `5d10!>8f<2kh3+7`

## Timing Expectations and Warnings

**CRITICAL TIMEOUT SPECIFICATIONS:**
- **NEVER CANCEL** any build, test, or format command
- Set timeouts to at least 30 seconds for all build/test operations
- Builds may produce warnings but should complete successfully
- Format operations detect existing whitespace issues but complete successfully

| Operation | Expected Time | Recommended Timeout |
|-----------|---------------|-------------------|
| `dotnet restore` | ~7 seconds | 60 seconds |
| `dotnet build` | ~8 seconds | 60 seconds |
| `dotnet test` | ~4-6 seconds | 60 seconds |
| `dotnet test` (with coverage) | ~6 seconds | 60 seconds |
| `dotnet format --verify-no-changes` | ~10 seconds | 60 seconds |
| `dotnet format` | ~10 seconds | 60 seconds |
| `dotnet pack` | ~2-3 seconds | 60 seconds |

## Known Issues and Workarounds
- **CS8618 Warning**: Non-nullable field '_random' warning in RandomIntGenerator.cs - this is existing and does not block builds
- **NUnit2005 Warnings**: Multiple Assert.AreEqual usage warnings in tests - these are style suggestions and do not block builds
- **Whitespace Issues**: `dotnet format --verify-no-changes` currently detects formatting issues in test files - run `dotnet format` to fix
- **No Executable Application**: This is a library project only - create console applications to test functionality manually

## CI/CD Integration
- **BuildAndTest.yml**: Runs on pushes to main/release branches and pull requests
- **PackAndPublish.yml**: Publishes to NuGet on v* tag pushes after successful builds
- **Coverage Target**: 80% code coverage (codecov.yml)
- **Build Matrix**: Ubuntu latest with .NET 8.x

Always ensure your changes pass the CI pipeline by running local validation steps before committing.