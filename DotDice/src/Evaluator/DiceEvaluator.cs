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
        /// This is a projection of <see cref="EvaluateDetailed"/> so both APIs
        /// are guaranteed to share one implementation and one set of semantics.
        /// </summary>
        public int Evaluate(Roll roll)
        {
            return EvaluateDetailed(roll).Value;
        }

        public DiceEvaluationResult EvaluateDetailed(Roll roll)
        {
            switch (roll)
            {
                case BasicRoll basicRoll:
                    return EvaluateBasicRollDetailed(basicRoll);
                case Constant constant:
                    return new DiceEvaluationResult(constant.Value, new List<DieEvent>());
                case ArithmeticRoll arithmeticRoll:
                    return EvaluateArithmeticRollDetailed(arithmeticRoll);
                default:
                    throw new ArgumentException("Unknown roll type", nameof(roll));
            }
        }

        private DiceEvaluationResult EvaluateBasicRollDetailed(BasicRoll basicRoll, int? groupId = null, ArithmeticOperator? groupOperator = null)
        {
            // Generation Phase: Create initial events
            var events = Enumerable.Range(0, basicRoll.NumberOfDice)
                .Select(_ => RollDieEvent(basicRoll.DieType, DieEventType.Initial, groupId, groupOperator))
                .ToList();

            // Apply modifiers in the proper order
            events = ApplyModifiersDetailed(events, basicRoll.Modifiers, basicRoll.DieType);

            // Calculate final value from events that are not dropped or discarded
            var finalValue = events
                .Where(e => e.Status != DieStatus.Dropped && e.Status != DieStatus.Discarded)
                .Sum(e => e.Value);

            return new DiceEvaluationResult(finalValue, events);
        }

        private DiceEvaluationResult EvaluateBasicRollDetailed(BasicRoll basicRoll)
        {
            return EvaluateBasicRollDetailed(basicRoll, null, null);
        }

        private DiceEvaluationResult EvaluateArithmeticRollDetailed(ArithmeticRoll arithmeticRoll)
        {
            int result = 0;
            var allEvents = new List<DieEvent>();
            int groupId = 0; // Assign unique group IDs

            foreach (var (operation, roll) in arithmeticRoll.Terms)
            {
                DiceEvaluationResult rollResult;

                // Evaluate the roll with group information
                if (roll is BasicRoll basicRoll)
                {
                    rollResult = EvaluateBasicRollDetailed(basicRoll, groupId, operation);
                }
                else
                {
                    rollResult = EvaluateDetailed(roll);
                }

                switch (operation)
                {
                    case ArithmeticOperator.Add:
                        result += rollResult.Value;
                        // For constants (like +3), create an event if there are no events
                        if (!rollResult.Events.Any() && roll is Constant constantRoll)
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
                        if (!rollResult.Events.Any() && roll is Constant constantRoll2)
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

                // For non-BasicRoll events (like constants from other sources), assign group info if missing
                var eventsToAdd = rollResult.Events.Select(e =>
                    e.GroupId.HasValue ? e : e with { GroupId = groupId, GroupOperator = operation }).ToList();

                allEvents.AddRange(eventsToAdd);
                groupId++; // Increment group ID for next term
            }

            return new DiceEvaluationResult(result, allEvents);
        }

        private DieEvent RollDieEvent(DieType dieType, DieEventType eventType, int? groupId = null, ArithmeticOperator? groupOperator = null)
        {
            var value = dieType switch
            {
                DieType.Basic roll => _rng.Next(1, roll.sides + 1),
                DieType.Reroll roll => _rng.Next(1, roll.sides + 1),
                DieType.Percent => _rng.Next(1, 101),
                DieType.Fudge => _rng.Next(-1, 2),
                _ => throw new ArgumentException("Unknown die type", nameof(dieType))
            };

            var significance = GetRollSignificance(value, dieType);

            return new DieEvent
            {
                Value = value,
                Type = eventType,
                DieType = dieType,
                Significance = significance,
                Status = DieStatus.Kept,
                Success = SuccessStatus.Neutral,
                GroupId = groupId,
                GroupOperator = groupOperator
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

        private List<DieEvent> ApplyModifiersDetailed(List<DieEvent> events, IEnumerable<Modifier> modifiers, DieType originalDieType)
        {
            // Phase 1: Generation Phase - Creates Events
            // Handle Initial rolls (already done), Reroll, then Explosion/Compound
            foreach (var modifier in modifiers)
            {
                switch (modifier)
                {
                    case RerollOnceModifier rerollOnceModifier:
                        events = ApplyRerollOnceModifierDetailed(events, rerollOnceModifier, originalDieType);
                        break;
                    case RerollMultipleModifier rerollUntilModifier:
                        events = ApplyRerollUntilModifierDetailed(events, rerollUntilModifier, originalDieType);
                        break;
                    case ExplodeModifier explodeModifier:
                        events = ApplyExplodeModifierDetailed(events, explodeModifier, originalDieType);
                        break;
                    case CompoundingModifier compoundingModifier:
                        events = ApplyCompoundingModifierDetailed(events, compoundingModifier, originalDieType);
                        break;
                }
            }

            // Phase 2: Modification Phase - Updates Events
            // Handle Keep/Drop modifiers by updating the Status property
            foreach (var modifier in modifiers)
            {
                switch (modifier)
                {
                    case KeepModifier keepModifier:
                        ApplyKeepModifierDetailed(events, keepModifier);
                        break;
                    case DropModifier dropModifier:
                        ApplyDropModifierDetailed(events, dropModifier);
                        break;
                }
            }

            // Phase 3: Finalization Phase - Reads Events
            // Success and failure counting are applied together against the same dice,
            // so expressions like "6d10>8f<2" produce (successes - failures).
            var successModifier = modifiers.OfType<SuccessModifier>().FirstOrDefault();
            var failureModifier = modifiers.OfType<FailureModifier>().FirstOrDefault();

            if (successModifier != null || failureModifier != null)
            {
                events = ApplySuccessFailureModifiersDetailed(events, successModifier, failureModifier);
            }

            foreach (var modifier in modifiers)
            {
                if (modifier is ConstantModifier constantModifier)
                {
                    events = ApplyConstantModifierDetailed(events, constantModifier);
                }
            }

            return events;
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

        #region Detailed Modifier Methods

        private List<DieEvent> ApplyRerollOnceModifierDetailed(List<DieEvent> events, RerollOnceModifier rerollOnceModifier, DieType originalDieType)
        {
            var result = new List<DieEvent>(events);

            for (int i = 0; i < events.Count; i++)
            {
                var evt = events[i];

                // Skip non-rollable events
                if (evt.Status == DieStatus.Discarded ||
                    !ShouldProcessEvent(evt))
                {
                    continue;
                }

                // Check if this event should be rerolled
                if (Compare(evt.Value, rerollOnceModifier.Operator, rerollOnceModifier.Value))
                {
                    // Mark the original as discarded
                    evt.Status = DieStatus.Discarded;

                    // Create a reroll event (preserve group information from original event)
                    var rerollEvent = RollDieEvent(originalDieType, DieEventType.Reroll, evt.GroupId, evt.GroupOperator);
                    result.Add(rerollEvent);
                }
            }

            return result;
        }

        private List<DieEvent> ApplyRerollUntilModifierDetailed(List<DieEvent> events, RerollMultipleModifier rerollUntilModifier, DieType originalDieType)
        {
            var result = new List<DieEvent>(events);

            for (int i = 0; i < events.Count; i++)
            {
                var evt = events[i];

                // Skip non-rollable events
                if (evt.Status == DieStatus.Discarded ||
                    !ShouldProcessEvent(evt))
                {
                    continue;
                }

                var currentEvent = evt;
                var maxRerolls = MaxRerolls;

                while (maxRerolls-- > 0 &&
                       Compare(currentEvent.Value, rerollUntilModifier.Operator, rerollUntilModifier.Value))
                {
                    // Mark current as discarded
                    currentEvent.Status = DieStatus.Discarded;

                    // Create a reroll event (preserve group information from original event)
                    currentEvent = RollDieEvent(originalDieType, DieEventType.Reroll, evt.GroupId, evt.GroupOperator);
                    result.Add(currentEvent);
                }
            }

            return result;
        }

        private List<DieEvent> ApplyExplodeModifierDetailed(List<DieEvent> events, ExplodeModifier explodeModifier, DieType originalDieType)
        {
            var result = new List<DieEvent>(events);

            // Process each original event for explosions
            for (int i = 0; i < events.Count; i++)
            {
                var evt = events[i];

                // Skip non-rollable events
                if (evt.Status == DieStatus.Discarded ||
                    !ShouldProcessEvent(evt))
                {
                    continue;
                }

                var currentEvent = evt;
                int explosionCounter = 0;

                while (explosionCounter < MaxExplosions &&
                       Compare(currentEvent.Value, explodeModifier.Operator, explodeModifier.Value))
                {
                    // Create explosion event (preserve group information from original event)
                    currentEvent = RollDieEvent(originalDieType, DieEventType.Explosion, evt.GroupId, evt.GroupOperator);
                    result.Add(currentEvent);

                    explosionCounter++;
                }
            }

            return result;
        }

        private List<DieEvent> ApplyCompoundingModifierDetailed(List<DieEvent> events, CompoundingModifier compoundingModifier, DieType originalDieType)
        {
            var result = new List<DieEvent>();

            foreach (var evt in events)
            {
                // Skip non-rollable events
                if (evt.Status == DieStatus.Discarded ||
                    !ShouldProcessEvent(evt))
                {
                    result.Add(evt);
                    continue;
                }

                // Set up for compounding - start with the original event's value
                int totalValue = evt.Value;
                int compoundCounter = 0;
                var currentValue = evt.Value;
                var compoundEvents = new List<DieEvent>();

                // Track compound events for transparency but use totalValue for final result
                while (compoundCounter < MaxCompounds &&
                       Compare(currentValue, compoundingModifier.Operator, compoundingModifier.Value))
                {
                    // Create compound event (preserve group information from original event)
                    var compoundEvent = RollDieEvent(originalDieType, DieEventType.Compound, evt.GroupId, evt.GroupOperator);
                    compoundEvents.Add(compoundEvent);

                    // Add to total value
                    totalValue += compoundEvent.Value;
                    currentValue = compoundEvent.Value;

                    compoundCounter++;
                }

                // Create a single event representing the compounded result
                var finalEvent = new DieEvent
                {
                    Value = totalValue,
                    Type = compoundEvents.Count > 0 ? DieEventType.Compound : evt.Type,
                    DieType = evt.DieType,
                    Significance = GetRollSignificance(totalValue, evt.DieType),
                    Status = evt.Status,
                    Success = evt.Success,
                    GroupId = evt.GroupId,
                    GroupOperator = evt.GroupOperator
                };

                result.Add(finalEvent);

                // Add the compound events for transparency (but mark them as discarded so they don't count in final sum)
                foreach (var compoundEvent in compoundEvents)
                {
                    compoundEvent.Status = DieStatus.Discarded;
                    result.Add(compoundEvent);
                }
            }

            return result;
        }

        private void ApplyKeepModifierDetailed(List<DieEvent> events, KeepModifier keepModifier)
        {
            ApplyKeepOrDropModifierDetailed(events, keepModifier.Count, keepModifier.KeepHighest, isKeep: true);
        }

        private void ApplyDropModifierDetailed(List<DieEvent> events, DropModifier dropModifier)
        {
            ApplyKeepOrDropModifierDetailed(events, dropModifier.Count, dropModifier.DropHighest, isKeep: false);
        }

        /// <summary>
        /// Marks dice kept or dropped by rank, selecting over pooled index buffers
        /// rather than materialising and ordering the events themselves.
        ///
        /// Selection is stable in original roll order: among dice of equal value, the
        /// earliest-rolled are selected first. That is not cosmetic pedantry. Which of
        /// two equal dice is marked <see cref="DieStatus.Dropped"/> is visible through
        /// <see cref="EvaluateDetailed"/>, and consumers render dropped dice
        /// differently. Array.Sort is not a stable sort, so stability here comes from
        /// the sort key rather than from the sort.
        /// </summary>
        private void ApplyKeepOrDropModifierDetailed(List<DieEvent> events, int count, bool selectHighest, bool isKeep)
        {
            var keyPool = ArrayPool<long>.Shared;
            // Sort keys pack the ranking value and the die's index into one long, so a
            // plain ascending sort of the keys yields the selection order directly and
            // the indices come back out of the low half.
            var keys = keyPool.Rent(events.Count);

            try
            {
                int rollable = 0;

                for (int i = 0; i < events.Count; i++)
                {
                    var evt = events[i];
                    if (evt.Status == DieStatus.Discarded || !ShouldProcessEvent(evt))
                    {
                        continue;
                    }

                    // Negating for keep-highest lets both directions use one ascending
                    // sort while leaving the index half ascending, which is what makes
                    // ties resolve in roll order either way. Widened before negating so
                    // the sign never wraps.
                    long rank = selectHighest ? -(long)evt.Value : evt.Value;
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
                            events[IndexOf(keys[i])].Status = DieStatus.Dropped;
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
                    events[IndexOf(keys[i])].Status = DieStatus.Dropped;
                }
            }
            finally
            {
                keyPool.Return(keys);
            }
        }

        /// <summary>Recovers the event index packed into the low half of a sort key.</summary>
        private static int IndexOf(long sortKey) => (int)(uint)sortKey;

        /// <summary>
        /// Applies success and/or failure counting against the same set of active dice.
        /// Each active die is compared against the success criteria first, then the failure
        /// criteria (a die can only count once; success takes precedence when criteria overlap).
        /// Returns a single count event whose value is (successes - failures), so:
        ///   success-only  => successCount
        ///   failure-only  => -failureCount
        ///   both          => successCount - failureCount (e.g. World of Darkness botch rules)
        /// </summary>
        private List<DieEvent> ApplySuccessFailureModifiersDetailed(List<DieEvent> events, SuccessModifier? successModifier, FailureModifier? failureModifier)
        {
            foreach (var evt in events)
            {
                if (evt.Status != DieStatus.Discarded &&
                    evt.Status != DieStatus.Dropped &&
                    ShouldProcessEvent(evt))
                {
                    if (successModifier != null && Compare(evt.Value, successModifier.Operator, successModifier.Value))
                    {
                        evt.Success = SuccessStatus.Success;
                    }
                    else if (failureModifier != null && Compare(evt.Value, failureModifier.Operator, failureModifier.Value))
                    {
                        evt.Success = SuccessStatus.Failure;
                    }
                }
            }

            var successCount = events.Count(e => e.Success == SuccessStatus.Success);
            var failureCount = events.Count(e => e.Success == SuccessStatus.Failure);

            return new List<DieEvent>
            {
                new DieEvent
                {
                    Value = successCount - failureCount,
                    Type = DieEventType.Initial,
                    Significance = RollSignificance.None,
                    Status = DieStatus.Kept,
                    Success = SuccessStatus.Neutral
                }
            };
        }

        private List<DieEvent> ApplyConstantModifierDetailed(List<DieEvent> events, ConstantModifier constantModifier)
        {
            var value = constantModifier.Operator switch
            {
                ArithmeticOperator.Add => constantModifier.Value,
                ArithmeticOperator.Subtract => -constantModifier.Value,
                _ => throw new InvalidEnumArgumentException("Invalid ArithmeticOperator")
            };

            var constantEvent = new DieEvent
            {
                Value = value,
                Type = DieEventType.Initial,
                Significance = RollSignificance.None,
                Status = DieStatus.Kept,
                Success = SuccessStatus.Neutral
            };

            return events.Append(constantEvent).ToList();
        }

        private static bool ShouldProcessEvent(DieEvent evt)
        {
            // Process events that represent actual dice rolls (not constants)
            // Constants would have Type = Initial but represent +N modifiers
            return evt.Type == DieEventType.Initial ||
                   evt.Type == DieEventType.Reroll ||
                   evt.Type == DieEventType.Explosion ||
                   evt.Type == DieEventType.Compound;
        }

        #endregion
    }
}
