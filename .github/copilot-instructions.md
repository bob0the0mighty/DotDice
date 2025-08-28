# DotDice - .NET Dice Rolling Library

DotDice is a flexible and extensible dice rolling library for .NET 8.0 applications, designed to support a wide variety of tabletop role-playing games (RPGs) and board games with extensive dice notation syntax.

**ALWAYS** reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Working Effectively

### Bootstrap and Build Process
Run these commands in exact order - **NEVER CANCEL** any long-running commands:
```bash
# Restore packages (takes ~8 seconds)
dotnet restore

# Build solution (takes ~8 seconds) 
dotnet build

# Run all tests (takes ~4 seconds, 350 tests)
dotnet test
```

### Build Timing and Requirements
- **NEVER CANCEL** build or test commands - actual timing measured:
  - `dotnet restore`: ~1-8 seconds (varies with network/cache)
  - `dotnet build`: ~5 seconds  
  - `dotnet test`: ~4 seconds (350 tests)
  - `dotnet pack`: ~1-2 seconds
- Set timeout to **60+ seconds** for build commands to be safe
- Set timeout to **30+ seconds** for test commands to be safe
- All commands are fast but network conditions may cause variation

### Packaging and Release
```bash
# Create NuGet package (takes ~2 seconds)
dotnet pack -c Release

# Package will be created at: DotDice/bin/Release/DotDice.1.0.0.nupkg
```

### Code Quality and Formatting
```bash
# Check formatting (will show errors if formatting needed)
dotnet format --verify-no-changes

# Auto-fix formatting issues 
dotnet format

# All formatting issues must be fixed before validation passes (as enforced by dotnet format --verify-no-changes)
```

## Validation Requirements

### Manual Testing Scenarios
**ALWAYS** test dice rolling functionality after making changes using this validation approach:

1. Create a simple console application in `/tmp/test_app/` with these test cases:
```csharp
// Basic rolls
"1d6"        // Single six-sided die
"3d6"        // Three six-sided dice  
"1d20+5"     // Twenty-sided die with modifier

// Advanced rolls
"2d20kh1"    // Advantage roll (keep highest)
"4d6kh3"     // Ability score (keep highest 3 of 4)
"5d6>4"      // Success counting (Shadowrun style)
"4d6!=6"     // Exploding dice (explode on 6)
"2d20dh1"    // Drop highest (disadvantage)
```

2. **CRITICAL**: After making any changes to parsing, evaluation, or core logic, run the test application and verify all roll expressions produce reasonable numeric results.

### Build Validation 
**ALWAYS** run these validation steps before committing changes:
```bash
# Ensure clean build
dotnet clean
dotnet restore  
dotnet build    # NEVER CANCEL - set 60+ second timeout
dotnet test     # NEVER CANCEL - set 30+ second timeout

# Verify packaging works
dotnet pack -c Release
```

## Codebase Navigation

### Project Structure
```
DotDice/                    # Main library project
├── src/
│   ├── Evaluator/         # DiceEvaluator.cs - Core evaluation logic
│   ├── Parser/            # DiceParser.cs, Rolls.cs, Modifiers.cs, Enums.cs
│   ├── Extension/         # StringExtensions.cs - Main API entry point
│   └── RandomNumberGenerator/  # IRandomNumberGenerator.cs, RandomIntGenerator.cs
└── DotDice.csproj         # Project file with NuGet packaging config

DotDiceTests/              # Test project  
├── src/
│   ├── Evaluator/         # DiceEvaluatorTests.cs
│   ├── Parser/            # DiceParserTests.cs  
│   └── Extension/         # StringExtensionsTests.cs
└── DotDiceTests.csproj    # Test project file

.github/workflows/         # CI/CD automation
├── BuildAndTest.yml       # Main CI pipeline
└── PackAndPublish.yml     # NuGet publishing workflow
```

### Key API Entry Points
- **Primary API**: `StringExtensions.ParseRoll()` - allows `"1d6".ParseRoll()`
- **Core Evaluator**: `DiceEvaluator.Evaluate()` - processes parsed roll objects
- **Parser**: `DiceParser.Roll.Parse()` - converts notation strings to roll objects

### Important Locations for Changes
- **String parsing**: `DotDice/src/Extension/StringExtensions.cs` 
- **Dice evaluation logic**: `DotDice/src/Evaluator/DiceEvaluator.cs`
- **Grammar parsing**: `DotDice/src/Parser/DiceParser.cs`
- **Roll definitions**: `DotDice/src/Parser/Rolls.cs`
- **Modifier implementations**: `DotDice/src/Parser/Modifiers.cs`

### Grammar Reference
The formal grammar is documented in `/grammar` - reference this for understanding valid dice notation syntax. Key patterns:
- `XdY` - X dice of Y sides
- `+N`, `-N` - Add/subtract constant
- `kh3`, `kl2` - Keep highest/lowest N dice
- `dh1`, `dl2` - Drop highest/lowest N dice  
- `>N`, `<N`, `=N` - Success counting with comparisons
- `!=N`, `^=N` - Exploding and compounding dice (explode/compound when roll equals N)
- `ro`, `rc` - Reroll once/continuously

## Common Development Tasks

### Adding New Dice Notation
1. Update the grammar in `/grammar` file if needed
2. Modify parser in `DotDice/src/Parser/DiceParser.cs`
3. Add new modifier class in `DotDice/src/Parser/Modifiers.cs` if needed
4. Update evaluator logic in `DotDice/src/Evaluator/DiceEvaluator.cs`
5. **ALWAYS** add comprehensive tests in appropriate test files
6. **ALWAYS** validate with manual test console application

### Debugging Issues
- Tests provide extensive coverage with 350 test cases
- Most issues will be in parsing (DiceParser.cs) or evaluation (DiceEvaluator.cs)  
- Use the test console app in `/tmp/test_app/` for quick validation
- Check existing test cases for similar patterns

### Performance Considerations
- Safety limits exist for infinite loops (MaxExplosions, MaxCompounds properties)
- Random number generation is controlled via IRandomNumberGenerator interface
- Tests use MockRandomNumberGenerator for deterministic results

## CI/CD Integration

### GitHub Actions Workflows
- **BuildAndTest.yml**: Runs on push/PR, executes `dotnet restore && dotnet build && dotnet test`
- **PackAndPublish.yml**: Triggered on v-tag releases, publishes to NuGet

### Workflow Compatibility
Any changes must pass:
1. `dotnet restore` - Package restoration  
2. `dotnet build` - Clean compilation
3. `dotnet test` - All 350 tests passing
4. Code formatting validation via `dotnet format --verify-no-changes`

## Dependencies and Requirements

### Runtime Requirements
- **.NET 8.0** - Target framework
- **Pidgin 3.3.0** - Parser combinator library (only external dependency)

### Development Requirements  
- .NET 8.0 SDK installed
- No additional tools required - uses standard dotnet CLI

### Package Information
- **Package ID**: DotDice
- **Current Version**: 1.0.0  
- **License**: MIT
- **Repository**: https://github.com/bob0the0mighty/DotDice

## Troubleshooting

### Common Issues
- **Build warnings about nullable fields**: Existing issue in RandomIntGenerator.cs - safe to ignore
- **NUnit analyzer warnings**: Existing code style warnings - focus on new code only
- **Formatting errors**: Run `dotnet format` to auto-fix, but existing code has some issues

### When Commands Fail
- **Build fails**: Check for syntax errors, ensure .NET 8.0 SDK is available
- **Tests fail**: Verify changes maintain existing behavior, check test output for specific failures
- **Pack fails**: Ensure clean build completed successfully first

Remember: **NEVER CANCEL** builds or tests - they complete quickly but set generous timeouts for network variations.