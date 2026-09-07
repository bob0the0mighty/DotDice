using System.Buffers;
using System.ComponentModel;
using DotDice.Parser;
using DotDice.RandomNumberGenerator;

namespace DotDice.Evaluator
{
    /// <summary>
    /// Evaluates a roll and returns the result.
    ///
    /// Evaluation follows a three-phase model:
    ///   Phase 1 (Generation): rerolls, explosions, and compounds create new die events.
    ///   Phase 2 (Modification): keep/drop modifiers mark events as dropped.
    ///   Phase 3 (Finalization): success/failure counting and constant modifiers are applied.
    /// Phases run in this order regardless of the order modifiers appear in the expression.
    ///
    /// The pipeline works over <see cref="DieSlot"/> values in a pooled buffer, not over
    /// <see cref="DieEvent"/> objects. There is still exactly one implementation of the
    /// semantics: <see cref="EvaluateDetailed"/> turns the finished slots into events,
    /// and <see cref="Evaluate"/> reads the total off the same slots without
    /// materialising anything.
    /// </summary>
    public class DiceEvaluator
    {
        private readonly IRandomNumberGenerator<int> _rng;
        // Safety limits to prevent infinite loops
        // Must be greater than 0 to allow at least one explosion or compound
        private int _maxExplosions = 100;
        public int MaxExplosions
        {
            get{ return _maxExplosions; }
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException("MaxExplosions must be greater than 0");
                }
                _maxExplosions = value;
            }
        }

        private int _maxCompounds = 100;
        public int MaxCompounds
        {
            get { return _maxCompounds; }
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException("MaxCompounds must be greater than 0");
                }
                _maxCompounds = value;
            }
        }

        private int _maxRerolls = 10;
        public int MaxRerolls
        {
            get { return _maxRerolls; }
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException("MaxRerolls must be greater than 0");
                }
                _maxRerolls = value;
            }
        }

        public DiceEvaluator(int? seed = null)
        {
            // Initialize with provided seed or a default time-dependent seed
            _rng = seed.HasValue ? new RandomIntGenerator(seed.Value) : new RandomIntGenerator();
        }

        public DiceEvaluator(IRandomNumberGenerator<int> rng)
        {
            _rng = rng;
        }

        /// <summary>
        /// Evaluates a roll and returns its final value.
        ///
        /// Runs the same pipeline as <see cref="EvaluateDetailed"/> over the same slots,
        /// and stops before building any events. A caller who wants only a total does
        /// not pay for per-die objects it will not read.
        /// </summary>
        public int Evaluate(Roll roll)
        {
            switch (roll)
            {
                case BasicRoll basicRoll:
                    return EvaluateBasicRollValue(basicRoll);
                case Constant constant:
                    return constant.Value;
                case ArithmeticRoll arithmeticRoll:
                    return EvaluateArithmeticRollValue(arithmeticRoll);
                default:
                    throw new ArgumentException("Unknown roll type", nameof(roll));
            }
        }

        public DiceEvaluationResult EvaluateDetailed(Roll roll)
        {
            switch (roll)
            {
                case BasicRoll basicRoll:
                    return EvaluateBasicRollDetailed(basicRoll, null, null);
                case Constant constant:
                    return new DiceEvaluationResult(constant.Value, new List<DieEvent>());
                case ArithmeticRoll arithmeticRoll:
                    return EvaluateArithmeticRollDetailed(arithmeticRoll);
                default:
                    throw new ArgumentException("Unknown roll type", nameof(roll));
            }
        }

        private int EvaluateBasicRollValue(BasicRoll basicRoll)
        {
            Span<DieSlot> buffer = stackalloc DieSlot[SlotList.StackCapacity];
            var slots = new SlotList(buffer, basicRoll.NumberOfDice);
            try
            {
                return EvaluateBasicRollIntoSlots(basicRoll, ref slots);
            }
            finally
            {
                slots.Return();
            }
        }

        private DiceEvaluationResult EvaluateBasicRollDetailed(BasicRoll basicRoll, int? groupId, ArithmeticOperator? groupOperator)
        {
            Span<DieSlot> buffer = stackalloc DieSlot[SlotList.StackCapacity];
            var slots = new SlotList(buffer, basicRoll.NumberOfDice);
            try
            {
                var value = EvaluateBasicRollIntoSlots(basicRoll, ref slots);
                var events = new List<DieEvent>(slots.Count);
                MaterialiseInto(events, ref slots, basicRoll.DieType, groupId, groupOperator);
                return new DiceEvaluationResult(value, events);
            }
            finally
            {
                slots.Return();
            }
        }

        /// <summary>
        /// Rolls the dice, applies every modifier, and returns the total. The finished
        /// slots are left in the buffer for a caller that wants to materialise them.
        /// </summary>
        private int EvaluateBasicRollIntoSlots(BasicRoll basicRoll, ref SlotList slots)
        {
            // Generation Phase: Create initial slots
            for (int i = 0; i < basicRoll.NumberOfDice; i++)
            {
                slots.Add(RollDieSlot(basicRoll.DieType, DieEventType.Initial));
            }

            ApplyModifiers(ref slots, basicRoll.Modifiers, basicRoll.DieType);

            // Calculate final value from slots that are not dropped or discarded
            int finalValue = 0;
            for (int i = 0; i < slots.Count; i++)
            {
                ref var slot = ref slots[i];
                if (slot.Status != DieStatus.Dropped && slot.Status != DieStatus.Discarded)
                {
                    finalValue += slot.Value;
                }
            }

            return finalValue;
        }

        /// <summary>
        /// Builds the public events for one basic roll's finished slots.
        ///
        /// The die type and group information are constant across the roll, so they are
        /// applied here rather than stored per slot. Synthetic slots (a success count, a
        /// constant modifier) carry none of the three, matching what the event pipeline
        /// produced for them.
        /// </summary>
        private static void MaterialiseInto(List<DieEvent> events, ref SlotList slots, DieType dieType, int? groupId, ArithmeticOperator? groupOperator)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                ref var slot = ref slots[i];
                events.Add(new DieEvent
                {
                    Value = slot.Value,
                    Type = slot.Type,
                    DieType = slot.IsDie ? dieType : null,
                    Significance = slot.Significance,
                    Status = slot.Status,
                    Success = slot.Success,
                    GroupId = groupId,
                    GroupOperator = groupOperator
                });
            }
        }

        private int EvaluateArithmeticRollValue(ArithmeticRoll arithmeticRoll)
        {
            int result = 0;

            foreach (var (operation, roll) in arithmeticRoll.Terms)
            {
                // Recursion, so each basic-roll term gets its own stack buffer in its
                // own frame rather than sharing one across the loop.
                var value = Evaluate(roll);

                switch (operation)
                {
                    case ArithmeticOperator.Add:
                        result += value;
                        break;
                    case ArithmeticOperator.Subtract:
                        result -= value;
                        break;
                    default:
                        throw new ArgumentException($"Unknown arithmetic operator: {operation}");
                }
            }

            return result;
        }

        private DiceEvaluationResult EvaluateArithmeticRollDetailed(ArithmeticRoll arithmeticRoll)
        {
            int result = 0;
            var allEvents = new List<DieEvent>();
            int groupId = 0; // Assign unique group IDs

            // One buffer reused across terms. Each term finishes with its slots before
            // the next starts, and hoisting it keeps the stack flat however many terms
            // the expression has.
            Span<DieSlot> buffer = stackalloc DieSlot[SlotList.StackCapacity];

            foreach (var (operation, roll) in arithmeticRoll.Terms)
            {
                if (roll is BasicRoll basicRoll)
                {
                    // Materialise this term's slots straight into the shared list, with
                    // the group information applied as they are built.
                    var slots = new SlotList(buffer, basicRoll.NumberOfDice);
                    try
                    {
                        var termValue = EvaluateBasicRollIntoSlots(basicRoll, ref slots);
                        result += operation == ArithmeticOperator.Add ? termValue : -termValue;
                        MaterialiseInto(allEvents, ref slots, basicRoll.DieType, groupId, operation);
                    }
                    finally
                    {
                        slots.Return();
                    }
                }
                else
                {
                    var rollResult = EvaluateDetailed(roll);

                    switch (operation)
                    {
                        case ArithmeticOperator.Add:
                            result += rollResult.Value;
                            // For constants (like +3), create an event if there are no events
                            if (rollResult.Events.Count == 0 && roll is Constant constantRoll)
                            {
                                allEvents.Add(new DieEvent
                                {
                                    Value = constantRoll.Value,
                                    Type = DieEventType.Initial,
                                    Significance = RollSignificance.None,
                                    Status = DieStatus.Kept,
                                    Success = SuccessStatus.Neutral,
                                    GroupId = groupId,
                                    GroupOperator = operation
                                });
                            }
                            break;
                        case ArithmeticOperator.Subtract:
                            result -= rollResult.Value;
                            // For constants (like -3), create an event if there are no events
                            if (rollResult.Events.Count == 0 && roll is Constant constantRoll2)
                            {
                                allEvents.Add(new DieEvent
                                {
                                    Value = -constantRoll2.Value,
                                    Type = DieEventType.Initial,
                                    Significance = RollSignificance.None,
                                    Status = DieStatus.Kept,
                                    Success = SuccessStatus.Neutral,
                                    GroupId = groupId,
                                    GroupOperator = operation
                                });
                            }
                            break;
                        default:
                            throw new ArgumentException($"Unknown arithmetic operator: {operation}");
                    }

                    // Nested rolls carry their own group information; anything without it
                    // takes this term's.
                    var termEvents = rollResult.Events;
                    for (int i = 0; i < termEvents.Count; i++)
                    {
                        var evt = termEvents[i];
                        allEvents.Add(evt.GroupId.HasValue
                            ? evt
                            : evt with { GroupId = groupId, GroupOperator = operation });
                    }
                }

                groupId++; // Increment group ID for next term
            }

            return new DiceEvaluationResult(result, allEvents);
        }

        private DieSlot RollDieSlot(DieType dieType, DieEventType eventType)
        {
            var value = dieType switch
            {
                DieType.Basic roll => _rng.Next(1, roll.sides + 1),
                DieType.Reroll roll => _rng.Next(1, roll.sides + 1),
                DieType.Percent => _rng.Next(1, 101),
                DieType.Fudge => _rng.Next(-1, 2),
                _ => throw new ArgumentException("Unknown die type", nameof(dieType))
            };

            return new DieSlot
            {
                Value = value,
                Type = eventType,
                Significance = GetRollSignificance(value, dieType),
                Status = DieStatus.Kept,
                Success = SuccessStatus.Neutral,
                IsDie = true
            };
        }

        private static RollSignificance GetRollSignificance(int value, DieType? dieType)
        {
            return dieType switch
            {
                DieType.Basic basic when value == 1 => RollSignificance.Minimum,
                DieType.Basic basic when value == basic.sides => RollSignificance.Maximum,
                DieType.Reroll reroll when value == 1 => RollSignificance.Minimum,
                DieType.Reroll reroll when value == reroll.sides => RollSignificance.Maximum,
                DieType.Percent when value == 1 => RollSignificance.Minimum,
                DieType.Percent when value == 100 => RollSignificance.Maximum,
                DieType.Fudge when value == -1 => RollSignificance.Minimum,
                DieType.Fudge when value == 1 => RollSignificance.Maximum,
                _ => RollSignificance.None
            };
        }

        private void ApplyModifiers(ref SlotList slots, IEnumerable<Modifier> modifiers, DieType originalDieType)
        {
            // The modifier list is walked four times below. Materialising it once and
            // indexing avoids an enumerator allocation per walk, which is per-evaluation
            // cost that does not scale with dice count and so dominates small rolls.
            var modifierList = modifiers as IReadOnlyList<Modifier> ?? modifiers.ToList();

            // Phase 1: Generation Phase - Creates Slots
            // Handle Initial rolls (already done), Reroll, then Explosion/Compound
            for (int m = 0; m < modifierList.Count; m++)
            {
                switch (modifierList[m])
                {
                    case RerollOnceModifier rerollOnceModifier:
                        ApplyRerollOnceModifier(ref slots, rerollOnceModifier, originalDieType);
                        break;
                    case RerollMultipleModifier rerollUntilModifier:
                        ApplyRerollUntilModifier(ref slots, rerollUntilModifier, originalDieType);
                        break;
                    case ExplodeModifier explodeModifier:
                        ApplyExplodeModifier(ref slots, explodeModifier, originalDieType);
                        break;
                    case CompoundingModifier compoundingModifier:
                        ApplyCompoundingModifier(ref slots, compoundingModifier, originalDieType);
                        break;
                }
            }

            // Phase 2: Modification Phase - Updates Slots
            // Handle Keep/Drop modifiers by updating the Status field
            for (int m = 0; m < modifierList.Count; m++)
            {
                switch (modifierList[m])
                {
                    case KeepModifier keepModifier:
                        ApplyKeepOrDropModifier(ref slots, keepModifier.Count, keepModifier.KeepHighest, isKeep: true);
                        break;
                    case DropModifier dropModifier:
                        ApplyKeepOrDropModifier(ref slots, dropModifier.Count, dropModifier.DropHighest, isKeep: false);
                        break;
                }
            }

            // Phase 3: Finalization Phase - Reads Slots
            // Success and failure counting are applied together against the same dice,
            // so expressions like "6d10>8f<2" produce (successes - failures).
            SuccessModifier? successModifier = null;
            FailureModifier? failureModifier = null;
            for (int m = 0; m < modifierList.Count; m++)
            {
                if (successModifier == null && modifierList[m] is SuccessModifier success)
                {
                    successModifier = success;
                }
                else if (failureModifier == null && modifierList[m] is FailureModifier failure)
                {
                    failureModifier = failure;
                }
            }

            if (successModifier != null || failureModifier != null)
            {
                ApplySuccessFailureModifiers(ref slots, successModifier, failureModifier);
            }

            for (int m = 0; m < modifierList.Count; m++)
            {
                if (modifierList[m] is ConstantModifier constantModifier)
                {
                    ApplyConstantModifier(ref slots, constantModifier);
                }
            }
        }

        private bool Compare(int rollResult, ComparisonOperator comparisonOperator, int modifierValue)
        {
            return comparisonOperator switch
            {
                ComparisonOperator.GreaterThan => rollResult > modifierValue,
                ComparisonOperator.LessThan => rollResult < modifierValue,
                ComparisonOperator.Equal => rollResult == modifierValue,
                _ => throw new InvalidEnumArgumentException("Invalid ComparisonOperator")
            };
        }

        #region Modifier Methods

        // Note on the loop bounds below: every generation modifier snapshots the slot
        // count before iterating. Rerolls and explosions do chain, in their own inner
        // loops, so a maximum face keeps exploding as long as it keeps rolling maximum.
        // The snapshot stops the outer loop from picking up a slot the inner loop
        // already carried to its conclusion and processing it a second time.
        //
        // A previously discarded slot is skipped. There is no other eligibility test:
        // the old ShouldProcessEvent listed all four DieEventType values and so was
        // always true.

        private void ApplyRerollOnceModifier(ref SlotList slots, RerollOnceModifier rerollOnceModifier, DieType originalDieType)
        {
            // Snapshotting the bound is what makes this reroll each die at most once. A
            // reroll must not itself be rerolled, or "ro" would mean "rc".
            int originalCount = slots.Count;

            for (int i = 0; i < originalCount; i++)
            {
                if (slots[i].Status == DieStatus.Discarded)
                {
                    continue;
                }

                if (Compare(slots[i].Value, rerollOnceModifier.Operator, rerollOnceModifier.Value))
                {
                    slots[i].Status = DieStatus.Discarded;
                    slots.Add(RollDieSlot(originalDieType, DieEventType.Reroll));
                }
            }
        }

        private void ApplyRerollUntilModifier(ref SlotList slots, RerollMultipleModifier rerollUntilModifier, DieType originalDieType)
        {
            int originalCount = slots.Count;

            for (int i = 0; i < originalCount; i++)
            {
                if (slots[i].Status == DieStatus.Discarded)
                {
                    continue;
                }

                int currentIndex = i;
                int currentValue = slots[i].Value;
                int remainingRerolls = MaxRerolls;

                while (remainingRerolls-- > 0 &&
                       Compare(currentValue, rerollUntilModifier.Operator, rerollUntilModifier.Value))
                {
                    slots[currentIndex].Status = DieStatus.Discarded;

                    var reroll = RollDieSlot(originalDieType, DieEventType.Reroll);
                    slots.Add(reroll);

                    currentIndex = slots.Count - 1;
                    currentValue = reroll.Value;
                }
            }
        }

        private void ApplyExplodeModifier(ref SlotList slots, ExplodeModifier explodeModifier, DieType originalDieType)
        {
            int originalCount = slots.Count;

            for (int i = 0; i < originalCount; i++)
            {
                if (slots[i].Status == DieStatus.Discarded)
                {
                    continue;
                }

                int currentValue = slots[i].Value;
                int explosionCounter = 0;

                while (explosionCounter < MaxExplosions &&
                       Compare(currentValue, explodeModifier.Operator, explodeModifier.Value))
                {
                    var explosion = RollDieSlot(originalDieType, DieEventType.Explosion);
                    slots.Add(explosion);

                    currentValue = explosion.Value;
                    explosionCounter++;
                }
            }
        }

        /// <summary>
        /// Folds each die's compound rolls into a single total, keeping the intermediate
        /// rolls as discarded slots for transparency.
        ///
        /// Unlike the other generation modifiers this one reorders: each die's combined
        /// slot is followed by that die's own compound rolls. Rebuilding into a second
        /// pooled buffer is what preserves that interleaving, since appending in place
        /// would push every compound roll to the end.
        /// </summary>
        private void ApplyCompoundingModifier(ref SlotList slots, CompoundingModifier compoundingModifier, DieType originalDieType)
        {
            // Pooled rather than stack-backed: this buffer replaces the caller's, so it
            // has to outlive this frame.
            var rebuilt = new SlotList(default, slots.Count);

            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];

                if (slot.Status == DieStatus.Discarded)
                {
                    rebuilt.Add(slot);
                    continue;
                }

                // The combined slot's value is not known until its compounds have been
                // rolled, so its position is reserved and filled in afterwards.
                int combinedIndex = rebuilt.Count;
                rebuilt.Add(slot);

                int totalValue = slot.Value;
                int currentValue = slot.Value;
                int compoundCounter = 0;

                while (compoundCounter < MaxCompounds &&
                       Compare(currentValue, compoundingModifier.Operator, compoundingModifier.Value))
                {
                    var compound = RollDieSlot(originalDieType, DieEventType.Compound);

                    totalValue += compound.Value;
                    currentValue = compound.Value;

                    // Kept in the output for transparency, discarded so it does not count
                    // toward the total a second time.
                    compound.Status = DieStatus.Discarded;
                    rebuilt.Add(compound);

                    compoundCounter++;
                }

                rebuilt[combinedIndex] = new DieSlot
                {
                    Value = totalValue,
                    Type = compoundCounter > 0 ? DieEventType.Compound : slot.Type,
                    Significance = GetRollSignificance(totalValue, originalDieType),
                    Status = slot.Status,
                    Success = slot.Success,
                    IsDie = slot.IsDie
                };
            }

            var replaced = slots;
            slots = rebuilt;
            replaced.Return();
        }

        /// <summary>
        /// Marks dice kept or dropped by rank, selecting over a pooled buffer of sort
        /// keys rather than ordering the dice themselves.
        ///
        /// Selection is stable in original roll order: among dice of equal value, the
        /// earliest-rolled are selected first. That is not cosmetic pedantry. Which of
        /// two equal dice is marked <see cref="DieStatus.Dropped"/> is visible through
        /// <see cref="EvaluateDetailed"/>, and consumers render dropped dice
        /// differently. Array.Sort is not a stable sort, so stability here comes from
        /// the sort key rather than from the sort.
        /// </summary>
        private void ApplyKeepOrDropModifier(ref SlotList slots, int count, bool selectHighest, bool isKeep)
        {
            var keyPool = ArrayPool<long>.Shared;
            // Sort keys pack the ranking value and the die's index into one long, so a
            // plain ascending sort of the keys yields the selection order directly and
            // the indices come back out of the low half.
            var keys = keyPool.Rent(slots.Count);

            try
            {
                int rollable = 0;

                for (int i = 0; i < slots.Count; i++)
                {
                    if (slots[i].Status == DieStatus.Discarded)
                    {
                        continue;
                    }

                    // Negating for keep-highest lets both directions use one ascending
                    // sort while leaving the index half ascending, which is what makes
                    // ties resolve in roll order either way. Widened before negating so
                    // the sign never wraps.
                    long rank = selectHighest ? -(long)slots[i].Value : slots[i].Value;
                    keys[rollable++] = (rank << 32) | (uint)i;
                }

                if (rollable <= count)
                {
                    // Fewer dice than the modifier selects: keeping them all is a no-op,
                    // dropping them all is not.
                    if (!isKeep)
                    {
                        for (int i = 0; i < rollable; i++)
                        {
                            slots[IndexOf(keys[i])].Status = DieStatus.Dropped;
                        }
                    }
                    return;
                }

                Array.Sort(keys, 0, rollable);

                // The sort partitions the dice at `count`: everything before the
                // boundary is selected, everything after it is not. Keep drops the far
                // side, drop drops the near side. Selected dice are left exactly as they
                // were rather than being set back to Kept, so a die already dropped by an
                // earlier modifier stays dropped.
                int from = isKeep ? count : 0;
                int to = isKeep ? rollable : count;

                for (int i = from; i < to; i++)
                {
                    slots[IndexOf(keys[i])].Status = DieStatus.Dropped;
                }
            }
            finally
            {
                keyPool.Return(keys);
            }
        }

        /// <summary>Recovers the slot index packed into the low half of a sort key.</summary>
        private static int IndexOf(long sortKey) => (int)(uint)sortKey;

        /// <summary>
        /// Applies success and/or failure counting against the same set of active dice.
        /// Each active die is compared against the success criteria first, then the failure
        /// criteria (a die can only count once; success takes precedence when criteria overlap).
        /// Replaces the slots with a single count slot whose value is (successes - failures), so:
        ///   success-only  => successCount
        ///   failure-only  => -failureCount
        ///   both          => successCount - failureCount (e.g. World of Darkness botch rules)
        /// </summary>
        private void ApplySuccessFailureModifiers(ref SlotList slots, SuccessModifier? successModifier, FailureModifier? failureModifier)
        {
            int successCount = 0;
            int failureCount = 0;

            for (int i = 0; i < slots.Count; i++)
            {
                ref var slot = ref slots[i];

                if (slot.Status == DieStatus.Discarded || slot.Status == DieStatus.Dropped)
                {
                    continue;
                }

                if (successModifier != null && Compare(slot.Value, successModifier.Operator, successModifier.Value))
                {
                    slot.Success = SuccessStatus.Success;
                    successCount++;
                }
                else if (failureModifier != null && Compare(slot.Value, failureModifier.Operator, failureModifier.Value))
                {
                    slot.Success = SuccessStatus.Failure;
                    failureCount++;
                }
            }

            slots.Clear();
            slots.Add(new DieSlot
            {
                Value = successCount - failureCount,
                Type = DieEventType.Initial,
                Significance = RollSignificance.None,
                Status = DieStatus.Kept,
                Success = SuccessStatus.Neutral,
                IsDie = false
            });
        }

        private void ApplyConstantModifier(ref SlotList slots, ConstantModifier constantModifier)
        {
            var value = constantModifier.Operator switch
            {
                ArithmeticOperator.Add => constantModifier.Value,
                ArithmeticOperator.Subtract => -constantModifier.Value,
                _ => throw new InvalidEnumArgumentException("Invalid ArithmeticOperator")
            };

            slots.Add(new DieSlot
            {
                Value = value,
                Type = DieEventType.Initial,
                Significance = RollSignificance.None,
                Status = DieStatus.Kept,
                Success = SuccessStatus.Neutral,
                IsDie = false
            });
        }

        #endregion
    }
}
