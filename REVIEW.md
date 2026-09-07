# DotDice Code Review

*Reviewed 2026-07-05 against `main` at `c6104ad` (the code containing the grouping API published as 1.4.1). No code was changed as part of the review itself.*

*Update: findings 1, 2, 4, 5 (via `Evaluate` delegation), 8, and 9 are addressed on the `dotDice` branch. See `CHANGELOG.md` v1.5.0 and `DotDiceTests/src/BugFixTests.cs`.*

## Summary

DotDice is a clean, well-scoped library: a Pidgin-based parser producing an immutable `Roll` AST, a `DiceEvaluator` with a simple path (`Evaluate → int`) and a detailed path (`EvaluateDetailed → DiceEvaluationResult` with per-die `DieEvent`s), and a pluggable `IRandomNumberGenerator`. Test coverage is substantial (~211 test cases including grouping and regression suites). The main structural risk is that the simple and detailed evaluators are two parallel ~400-line implementations of the same semantics, and they have already diverged; one divergence is a genuine correctness bug (finding 1).

## High priority

### 1. Reroll uses the previous roll's *result* as the die's side count (simple path)

`DiceEvaluator.ApplyRerollOnceModifier` and `ApplyRerollUntilModifier` reroll via `RollDie(new DieType.Reroll(roll.result))`, passing the previous roll's **value** as the number of sides (`DiceEvaluator.cs:491` and `:516`). So `4d6ro=1` rerolls a 1 on a *one-sided* die, producing another 1 forever; a 3 would be rerolled on a d3. The detailed path is correct (it rerolls `originalDieType`), which means `ParseRoll` and `ParseRollDetailed` produce different distributions for any reroll expression. Fix: `new DieType.Reroll(originalDieType sides)`. The modifier apply methods need the original die type passed in, as the detailed path already does.

Why tests miss it: the mock RNG returns scripted values regardless of the requested range, so the wrong side count is invisible. Add a seeded-real-RNG property test asserting every rerolled value is within `1..sides`.

### 2. Combining success and failure modifiers is broken in both paths

For `6d10>8f<2` (World of Darkness-style: successes minus botches), Phase 3 of the detailed path applies the success modifier first, which **replaces the entire event list with a single neutral count event**, so the failure modifier then evaluates against that count, not the original dice. The simple path has the mirrored problem: `ApplyFailureModifier` returns a list containing only `-failureCount`, discarding the success count entirely, so the expression evaluates to `-failures` (or 0) regardless of successes. The parser accepts this syntax and `DiceParserTests.cs:406` tests the parse, but no test evaluates it. Correct semantics: count both against the same original dice and return `successes - failures`. This wants a combined finalization step rather than sequential list-replacing modifiers.

### 3. Version-story hygiene

The csproj says `<Version>1.3.0` while NuGet is at 1.4.1 (version presumably comes from the v-tag CI flow), and `CHANGELOG.md` stops at 1.0.1 despite four releases since. Recommended: make the csproj `<Version>` match the released version (or document that tags are authoritative), backfill the changelog, and keep it updated as part of the release workflow. Deleting merged `copilot/*` branches would also prevent accidentally working from a stale checkout, which is how this review initially started on outdated code.

### 4. Grammar has no shorthand for the most common VTT notations

`!`, `^`, `ro`, and `rc` all *require* an explicit comparison point (`3d6!=6`, `4d6ro=1`). Every major VTT (Roll20, Foundry) accepts `3d6!` (explode on max) and `ro1`/`r1` (reroll 1s, `=` implied). The consuming app's keypad and reference page emit exactly these unsupported shorthands. Recommend making the comparison point optional: `!` and `^` default to `= max face`, and a bare integer after `ro`/`rc` implies `=`. This is additive to the grammar (the `grammar` file's `<comparison_point>` gains an optional form) and unblocks the app without app-side workarounds.

## Medium priority

### 5. Two parallel evaluators guarantee future divergence

Findings 1 and 2, plus the drop-modifier duplication bug fixed earlier in `f4d995a`, are all instances of the same root cause: every modifier is implemented twice (`ApplyXxxModifier` and `ApplyXxxModifierDetailed`). The cheapest structural fix is to make the simple path a projection of the detailed one, `Evaluate(roll) => EvaluateDetailed(roll).Value`, and delete the ~400 lines of non-detailed modifier code. The detailed path allocates more, but dice expressions are small; if a hot path emerges (e.g. Monte Carlo simulation), optimize then, behind the same semantics. Short of that, add differential tests asserting both paths agree for a large corpus of seeded expressions.

### 6. Success/failure detailed events lose per-die transparency

`ApplySuccessModifierDetailed` marks individual dice as `Success` but then returns only the single count event, so consumers can't render which dice succeeded. That is inconsistent with the compounding modifier, which appends its intermediate rolls as `Discarded` events for transparency. Keep the marked original events in the list (as `Discarded`) alongside the count event, like compounding does. Same for failure.

### 7. `DieType.Explode` is declared but never constructed

Detailed explosion/reroll events carry the original die type and are distinguished only by `DieEventType`. That's a fine design, but the unused `DieType.Explode(int sides)` record misleads consumers. DiceDiceBaby currently checks `e.DieType is DieType.Explode` and gets dead code. Either delete the record or start assigning it; if deleting, note in the README that `DieEvent.Type` is the discriminator. Related: `DieType.Reroll` *is* constructed in the simple path (finding 1) but never in the detailed path; after fixing finding 1 it may be removable too, simplifying the type to actual die kinds.

### 8. Inconsistent loop-guard configuration

`MaxExplosions` and `MaxCompounds` are configurable properties (default 100) with validation; the reroll-until cap is a hard-coded `maxRerolls = 10` local in both paths. Promote it to a matching `MaxRerolls` property.

### 9. Parse errors are swallowed

`StringExtensions` throws `FormatException("Invalid roll format.")`, discarding Pidgin's error (position, expected tokens). Surfacing `result.Error` in the exception message would let consumers show "expected '=' , '<' or '>' after 'ro'" instead of a generic failure, directly useful for the app's keypad UX.

### 10. Redundant representations of constants

`2d6+3` parses as an `ArithmeticRoll` with a `Constant` term (since `Try(arithmeticRoll)` runs first), so `ConstantModifier` is nearly unreachable from the top-level parser; it only applies inside a lone `basicRoll` parse, which the arithmetic path shadows. Two code paths produce the same semantics with different ASTs and different event shapes. Consider removing `ConstantModifier` from the surface grammar (keep it for the programmatic API if desired) or unifying the event output.

## Low priority / polish

**RNG interface**: `IRandomNumberGenerator<N>` is generic over `INumber<N>` but `SetSeed`/`GetSeed` are `int`-typed, only one implementation exists, and `GetSeed` throws if unseeded. A plain `IRandomNumberGenerator` with `int Next(int minInclusive, int maxExclusive)` would cover every use in the evaluator; document that the exclusive-max convention matches `System.Random`. Also worth documenting that `System.Random` (and hence the evaluator) is not thread-safe.

**Comparison operators**: only `=`, `>`, `<`. `>=`/`<=` are common in VTT syntax and trivially added to `comparisonOperator`.

**Grammar file drift**: `grammar` documents a `sort_modifier` that isn't implemented and doesn't mention the arithmetic-expression layer's exclusion of constant modifiers. Either implement sort or trim the file; consider moving the (corrected) grammar into the README, which currently claims features (e.g. detailed `evt.Type` naming) slightly out of sync with the code.

**Whitespace tolerance**: `Tok` wrapping means `2 d 6 kh 2`-style spacing is accepted in some positions and not others (`Char('d')` isn't tokenized). Harmless, but pick a rule (probably: no internal whitespace) and test it.

**Test suggestions**: differential simple-vs-detailed tests (finding 5); seeded-real-RNG range assertions (finding 1); evaluation tests for `>Nf<M` combinations (finding 2); a shared corpus of expressions from the DiceDiceBaby reference page so the two repos can't drift apart silently.

## What's in good shape

The parser is declarative and easy to extend; the AST records are immutable and self-describing with useful `ToString()`s; the three-phase modifier ordering in the detailed evaluator (generate → modify → finalize) is a sound design and correctly documented in comments; explosion/compound loop guards prevent the classic infinite-explode hang; the grouping API (`GroupId`/`GroupOperator`) is well-designed for consumers rendering arithmetic expressions, with reroll/explode/compound events correctly inheriting their group; and the keep/drop reference-equality fix plus the `ApplyKeepOrDropModifier` consolidation on `origin/main` show the duplication problem is already being chipped away, and finding 5 just proposes finishing the job.
