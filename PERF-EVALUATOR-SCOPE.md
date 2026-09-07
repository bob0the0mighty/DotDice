# Scope: allocation-free evaluation

## Why

`DiceEvaluator.Evaluate` is a projection of `EvaluateDetailed`, so a caller who
wants only a total still pays for a fully materialised `List<DieEvent>`. Every
die is a heap-allocated `record`, and each modifier phase copies the list again.

Measured with `GC.GetAllocatedBytesForCurrentThread`:

| expression | dice | bytes/evaluation | bytes/die |
|---|---|---|---|
| `1d20` | 1 | 544 | 544 |
| `2d20kh1+5` | 2 | 2,184 | 1,092 |
| `4d6dl1` | 4 | 1,472 | 368 |
| `10d6!` | 10 | 1,808 | 181 |
| `100d20` | 100 | 7,672 | 77 |
| `500d20` | 500 | 36,472 | 73 |

This surfaced from a real complaint. Crits & Counters runs Monte Carlo
simulations over a favourite; a 100,000-iteration run of `500d20` allocates
**3.4 GB**, and a 1,000,000-iteration run **34 GB**. On desktop the GC absorbs
it (14 gen0 + 2 gen1 collections per 20,000 iterations). On a Pixel 9 Pro it
does not: the run crawls and stalls periodically, and throughput lands about
68× below desktop, far more than the hardware gap accounts for.

Allocation, not arithmetic, is the cost.

## What it buys

Prototype: roll into a pooled array of value-type slots, mark keep/drop status
in place, sum. Only basic rolls plus keep/drop, so it brackets the real change
rather than substituting for it.

| expression | current | slot-based | speedup | current alloc | slot alloc |
|---|---|---|---|---|---|
| `1d20` | 693 ns | 43 ns | **16.3×** | 544 B | **0 B** |
| `100d20` | 7,897 ns | 1,021 ns | **7.7×** | 7,672 B | **0 B** |
| `500d20` | 23,824 ns | 4,854 ns | **4.9×** | 36,472 B | **0 B** |
| `4d6dl1` | 755 ns | 93 ns | **8.1×** | 1,472 B | 80 B |
| `20d6kh5` | 2,972 ns | 494 ns | **6.0×** | 3,768 B | 208 B |

The keep/drop residual is the prototype's own index arrays, which the real
implementation pools too.

These are **desktop** numbers. On device the win should be larger, because the
GC pressure that dominates there is exactly what disappears. But that is
reasoning, not measurement. Nothing here has been measured on a phone.

## Design

**One implementation, not two.** The obvious shortcut, a separate
`EvaluateValue` that skips events, reintroduces the exact hazard the current
code's comment calls out: two implementations of one set of semantics, free to
drift on explosion limits, reroll ordering, keep/drop tie-breaks and
success/failure interaction. Rejected.

Instead, change the *internal working representation* and keep one pipeline:

- `DieSlot`, an internal mutable struct holding what `DieEvent` holds
  (value, type, significance, status, success, group id, group operator).
- The pipeline works over a `Span<DieSlot>` backed by an `ArrayPool<DieSlot>`
  rental, which the evaluator owns for the duration of one evaluation.
- `EvaluateDetailed` materialises `List<DieEvent>` from the slots at the very
  end, with the same output as today and the same allocation cost as today.
- `Evaluate` sums the kept slots and returns, never materialising anything.

The public API is unchanged: `DieEvent`, `DiceEvaluationResult`,
`EvaluateDetailed` and `Evaluate` all keep their current shapes and semantics.
This is not a breaking change.

## Hazards

**Keep/drop tie-breaking is currently stable, and must stay stable.**
`ApplyKeepOrDropModifierDetailed` uses `OrderBy`/`OrderByDescending`, which LINQ
guarantees to be stable, then tracks selections in a
`HashSet<DieEvent>(ReferenceEqualityComparer.Instance)`. Index-based selection
over a span must sort by `(value, originalIndex)`; `Array.Sort` alone is **not**
stable. The total is unaffected when values tie, but *which* die is marked
`Dropped` is observable through `EvaluateDetailed`, and Crits & Counters renders
it: dropped dice show in parentheses with a strikethrough. Getting this wrong
is a silent cosmetic regression in a downstream app.

**RNG call order must be preserved exactly. This is the hardest constraint,
not a footnote.** Not merely the same *number* of draws: the same methods, with
the same arguments, in the same order. `Next(1, sides + 1)` and `Next(sides) + 1`
cover the same range through different internal mapping in `System.Random`, so
they carry different modulo-bias characteristics.

Crits & Counters runs Monte Carlo simulations specifically to observe what its
real RNG does, which only means anything if the simulated path consumes
randomness the way the real roll path does. Perturbing call order would not
break any total, and would silently make the simulator test an RNG path the app
never executes.

`MockRandomNumberGenerator` feeds a scripted list, so the 570 existing tests
already pin this. That is the single strongest argument for doing the work here
rather than reimplementing a fast path downstream: the harness that enforces the
property that matters already exists, and any reordering fails loudly instead of
quietly changing what a seed produces.

**Explosions grow the working set.** `MaxExplosions`/`MaxCompounds` are 100
each, so `500d20!` can reach ~50,500 slots (~800 KB at 16 bytes) and DotDice has
no cap on dice count. The buffer needs a doubling growth path, still pooled.

**`ReferenceEqualityComparer` disappears.** Anything relying on `DieEvent`
reference identity inside the evaluator goes away with the rewrite; the
materialised events are fresh objects, which is already true today.

## Steps

Each builds and leaves the suite green.

1. **Differential and allocation tests, before touching the evaluator.**
   Over a corpus of expressions × scripted RNG scripts, assert
   `Evaluate(r) == EvaluateDetailed(r).Value`, and assert per-evaluation
   allocation against a budget with `GC.GetAllocatedBytesForCurrentThread`.
   This is what makes steps 2-5 safe, and it fails today only on the budget.
2. **Introduce `DieSlot` and the pooled working set**, with `EvaluateDetailed`
   materialising from it. Behaviour identical; all 570 tests stay green. This is
   the bulk of the work, effectively a rewrite of the evaluator's internals.
3. **Index-based, stable keep/drop.** Removes `OrderBy` and the identity
   `HashSet`. Needs its own tie-break test with duplicate values.
4. **De-LINQ the remaining per-evaluation paths**: `Where().Sum()`,
   `Select(... with {...}).ToList()`, and the `OfType<T>().FirstOrDefault()`
   modifier scans that run on every evaluation.
5. **`Evaluate` returns without materialising.** The payoff commit; the
   allocation budget from step 1 drops to zero.
6. **Benchmarks.** `DotDiceBenchmarks` already exists on `feature/benchmarks`
   with `[MemoryDiagnoser]`; add the dice-count sweep and record before/after.

## Size

Large. The evaluator is 606 lines and steps 2-4 rewrite most of its internals,
behind an unchanged public surface. The 570-test suite plus the differential
test from step 1 is what makes that tractable rather than reckless.

## Shipping

Crits & Counters consumes `<PackageReference Include="DotDice" Version="1.5.0" />`,
so the app sees none of this until a new package exists. Either publish 1.6.0,
or point the app at a local build while validating on device. Validating on the
Pixel before publishing is worth the detour. The phone is the platform this is
for, and it is the one platform none of the numbers above come from.

No change is needed in the app itself: it already calls `Evaluate`.
