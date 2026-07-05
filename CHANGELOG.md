* V1.0.0 - Initial Release
* V1.0.1 - Added readme to package
* V1.3.0 - Event-driven detailed evaluation (`ParseRollDetailed`, `DiceEvaluationResult`, `DieEvent`); backward-compatible `ParseRoll`.
* V1.4.1 - Arithmetic roll expressions (`3d20+5d6-1d4+1`) with per-term grouping metadata (`DieEvent.GroupId`, `DieEvent.GroupOperator`); keep/drop refactor and reference-equality fix.
* V1.5.0 - Bug fixes and grammar shorthands:
  * **Fixed**: `Evaluate()`/`ParseRoll` rerolled dice using the previous roll's *result* as the die's side count (e.g. `4d6ro=1` rerolled a 1 on a 1-sided die). Rerolls now use the original die.
  * **Fixed**: combining success and failure counting (e.g. `6d10>8f<2`) now returns successes minus failures counted against the same dice; previously the two modifiers clobbered each other in both evaluation paths. When a die matches both criteria, success takes precedence.
  * **Changed**: `Evaluate()` is now a projection of `EvaluateDetailed()` — one implementation, one set of semantics. As a result, keep/drop written *before* explode/reroll/compound now behaves per the documented phase model (generation, then keep/drop, then finalization), matching `ParseRollDetailed` and common VTT behavior. Expressions that relied on the old order-sensitive simple path (e.g. `2d20kh1!=20` exploding only the kept die) will see different results.
  * **Added**: grammar shorthands — bare `!` and `^` explode/compound on the die's maximum face; `!!` is an alias for `^`; a bare integer after `ro`, `rc`, `!`, or `^` means equality (`ro1` == `ro=1`, `3d6!6` == `3d6!=6`).
  * **Added**: `DiceEvaluator.MaxRerolls` property (default 10), matching the existing `MaxExplosions`/`MaxCompounds` loop guards.
  * **Improved**: `FormatException` from `ParseRoll`/`ParseRollDetailed` now includes the parser diagnostic (position and expected tokens).
