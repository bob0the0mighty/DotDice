using System.ComponentModel;
using DotDice.Parser;
using DotDice.RandomNumberGenerator;

namespace DotDice.Evaluator
{
    /// <summary>
    /// Evaluates a roll and returns the result.
    /// </summary>
    public class DiceEvaluator
    {
        private readonly IRandomNumberGenerator<int> _rng;
        private record rollResult(int result, DieType type);
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

        public DiceEvaluator(int? seed = null)
        {
            // Initialize with provided seed or a default time-dependent seed
            _rng = seed.HasValue ? new RandomIntGenerator(seed.Value) : new RandomIntGenerator();
        }

        public DiceEvaluator(IRandomNumberGenerator<int> rng)
        {
            _rng = rng;
        }

        public int Evaluate(Roll roll)
        {
            switch (roll)
            {
                case BasicRoll basicRoll:
                    return EvaluateBasicRoll(basicRoll);
                case Constant constant:
                    return constant.Value;
                case ArithmeticRoll arithmeticRoll:
                    return EvaluateArithmeticRoll(arithmeticRoll);
                default:
                    throw new ArgumentException("Unknown roll type", nameof(roll));
            }
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

        private int EvaluateBasicRoll(BasicRoll basicRoll)
        {
            // Generate random numbers for each die
            var rolls = Enumerable.Range(0, basicRoll.NumberOfDice)
                .Select(_ => RollDie(basicRoll.DieType))
                .ToList();

            // Apply modifiers
            rolls = ApplyModifiers(rolls, basicRoll.Modifiers);

            // Return the sum of the rolls
            return rolls.Sum(r => r.result);
        }

        private DiceEvaluationResult EvaluateBasicRollDetailed(BasicRoll basicRoll)
        {
            return EvaluateBasicRollDetailed(basicRoll, null, null);
        }

        private int EvaluateArithmeticRoll(ArithmeticRoll arithmeticRoll)
        {
            int result = 0;
            
            foreach (var (operation, roll) in arithmeticRoll.Terms)
            {
                int rollValue = Evaluate(roll);
                
                switch (operation)
                {
                    case ArithmeticOperator.Add:
                        result += rollValue;
                        break;
                    case ArithmeticOperator.Subtract:
                        result -= rollValue;
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

        private rollResult RollDie(DieType dieType)
        {
            return dieType switch
            {
                DieType.Basic roll => new(_rng.Next(1, roll.sides + 1), dieType),
                DieType.Reroll roll => new(_rng.Next(1, roll.sides + 1), dieType),
                DieType.Percent => new(_rng.Next(1, 101), dieType),
                DieType.Fudge => new(_rng.Next(-1, 2), dieType),
                _ => throw new ArgumentException("Unknown die type", nameof(dieType))
            };
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

        private static RollSignificance GetRollSignificance(int value, DieType dieType)
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

        private List<rollResult> ApplyModifiers(List<rollResult> rolls, IEnumerable<Modifier> modifiers)
        {
            foreach (var modifier in modifiers)
            {
                switch (modifier)
                {
                    case CompoundingModifier compoundingModifier:
                        rolls = ApplyCompoundingModifier(rolls, compoundingModifier);
                        break;
                    case ConstantModifier constantModifier:
                        rolls = ApplyConstantModifier(rolls, constantModifier);
                        break;
                    case DropModifier dropModifier:
                        rolls = ApplyDropModifier(rolls, dropModifier);
                        break;
                    case ExplodeModifier explodeModifier:
                        rolls = ApplyExplodeModifier(rolls, explodeModifier);
                        break;
                    case FailureModifier failureModifier:
                        rolls = ApplyFailureModifier(rolls, failureModifier);
                        break;
                    case KeepModifier keepModifier:
                        rolls = ApplyKeepModifier(rolls, keepModifier);
                        break;
                    case RerollOnceModifier rerollOnceModifier:
                        rolls = ApplyRerollOnceModifier(rolls, rerollOnceModifier);
                        break;
                    case RerollMultipleModifier rerollUntilModifier:
                        rolls = ApplyRerollUntilModifier(rolls, rerollUntilModifier);
                        break;
                    case SuccessModifier successModifier:
                        rolls = ApplySuccessModifier(rolls, successModifier);
                        break;
                }
            }
            return rolls;
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
            // Apply Success/Failure interpretations and handle constants
            foreach (var modifier in modifiers)
            {
                switch (modifier)
                {
                    case SuccessModifier successModifier:
                        events = ApplySuccessModifierDetailed(events, successModifier);
                        break;
                    case FailureModifier failureModifier:
                        events = ApplyFailureModifierDetailed(events, failureModifier);
                        break;
                    case ConstantModifier constantModifier:
                        events = ApplyConstantModifierDetailed(events, constantModifier);
                        break;
                }
            }

            return events;
        }

        /// <summary>
        /// Compounding modifier is similar to explode, but instead of adding new dice,
        /// it adds the result of additional rolls to the original die's result.
        /// For example, if rolling a d6 and the condition is >= 6, and you roll a 6,
        /// you roll another d6 and add its result to the original 6, continuing if that roll also meets the condition.
        /// To prevent infinite loops, we limit the number of compounds to 100 per die by default.
        /// </summary>
        /// <param name="rolls">The original roll results</param>
        /// <param name="compoundingModifier">The compounding modifier containing the comparison criteria</param>
        /// <returns>Updated list of rollResults with compounded values</returns>
        private List<rollResult> ApplyCompoundingModifier(List<rollResult> rolls, CompoundingModifier compoundingModifier)
        {
            var result = new List<rollResult>();

            // Process each original roll
            foreach (var roll in rolls)
            {
                // Skip static dice types (constant, success)
                if (roll.type is DieType.Constant || roll.type is DieType.Success)
                {
                    result.Add(roll);
                    continue;
                }

                // Set up for compounding
                int totalValue = roll.result;
                int compoundCounter = 0;
                var currentRoll = roll;

                // As long as we meet the condition and haven't hit the limit, keep compounding
                while (compoundCounter < MaxCompounds &&
                    Compare(currentRoll.result, compoundingModifier.Operator, compoundingModifier.Value))
                {
                    // Roll another die of the same type
                    DieType newDieType = currentRoll.type switch
                    {
                        DieType.Basic basic => new DieType.Basic(basic.sides),
                        DieType.Percent => new DieType.Percent(),
                        DieType.Fudge => new DieType.Fudge(),
                        _ => currentRoll.type // Use existing type if not specifically handled
                    };

                    // Roll the new die and add its value to the total
                    currentRoll = RollDie(newDieType);
                    totalValue += currentRoll.result;

                    // Increment the counter to prevent infinite loops
                    compoundCounter++;
                }

                // Add a single roll with the compounded value
                result.Add(new rollResult(totalValue, roll.type));
            }
            return result;
        }

        private List<rollResult> ApplyKeepModifier(List<rollResult> rolls, KeepModifier keepModifier)
        {
            return ApplyKeepOrDropModifier(rolls, keepModifier.Count, keepModifier.KeepHighest, isKeep: true);
        }

        private List<rollResult> ApplyDropModifier(List<rollResult> rolls, DropModifier dropModifier)
        {
            return ApplyKeepOrDropModifier(rolls, dropModifier.Count, dropModifier.DropHighest, isKeep: false);
        }

        private List<rollResult> ApplyKeepOrDropModifier(List<rollResult> rolls, int count, bool selectHighest, bool isKeep)
        {
            var staticRolls = rolls.Where(x => x.type is DieType.Constant || x.type is DieType.Success)
                .ToList();
            var dynamicRolls = rolls.Where(x => x.type is not DieType.Constant && x.type is not DieType.Success)
                .ToList();

            var ordered = selectHighest 
                ? dynamicRolls.OrderByDescending(x => x.result)
                : dynamicRolls.OrderBy(x => x.result);

            var selected = isKeep 
                ? ordered.Take(count)
                : ordered.Skip(count);

            return selected.Concat(staticRolls).ToList();
        }

        private List<rollResult> ApplyConstantModifier(List<rollResult> rolls, ConstantModifier constantModifier)
        {
            //Constant should be applied after everything else, and just add a value to the total of all rolls, not individual rolls
            var value = constantModifier.Operator switch
            {
                ArithmeticOperator.Add => constantModifier.Value,
                ArithmeticOperator.Subtract => -constantModifier.Value,
                _ => throw new InvalidEnumArgumentException("Invalid ArithmeticOperator")
            };
            return rolls.Append(new(value, new DieType.Constant()))
                .ToList();
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

        /// <summary>
        /// Reroll once modifier will reroll any dice that meets the condition once.
        /// </summary>
        /// <param name="rolls"></param>
        /// <param name="rerollOnceModifier"></param>
        /// <returns>Updated list of rollResult</returns>
        private List<rollResult> ApplyRerollOnceModifier(List<rollResult> rolls, RerollOnceModifier rerollOnceModifier)
        {
            return rolls.Select(roll =>
            {
                if (roll.type is DieType.Constant || roll.type is DieType.Success)
                {
                    return roll;
                }
                var comparison = Compare(roll.result, rerollOnceModifier.Operator, rerollOnceModifier.Value);
                return comparison ? RollDie(new DieType.Reroll(roll.result)) : roll;
            })
                .ToList();
        }

        /// <summary>
        /// Reroll once modifier will reroll any dice that meets the condition until success.
        /// In order to avoid infinite loop, we need to limit the number of rerolls.
        /// </summary>
        /// <param name="rolls"></param>
        /// <param name="rerollOnceModifier"></param>
        /// <returns>Updated list of rollResults with re-rolled values</returns>
        private List<rollResult> ApplyRerollUntilModifier(List<rollResult> rolls, RerollMultipleModifier rerollUntilModifier)
        {
            return rolls.Select(roll =>
            {
                if (roll.type is DieType.Constant || roll.type is DieType.Success)
                {
                    return roll;
                }

                var comparison = Compare(roll.result, rerollUntilModifier.Operator, rerollUntilModifier.Value);
                var maxRerolls = 10;
                while (comparison && maxRerolls-- > 0)
                {
                    roll = RollDie(new DieType.Reroll(roll.result));
                    comparison = Compare(roll.result, rerollUntilModifier.Operator, rerollUntilModifier.Value);
                }
                return roll;
            })
            .ToList();
        }

        /// <summary>
        /// Explode modifier will add additional dice for each die that meets the condition.
        /// For example, if the condition is > 5 on a d6, and you roll a 6, then you get to roll
        /// another d6 and add it to your total. If that d6 also rolls a 6, you roll again, etc.
        /// To prevent infinite loops, we limit the number of explosions to 100 per die by default.
        /// </summary>
        /// <param name="rolls">The original roll results</param>
        /// <param name="explodeModifier">The explode modifier containing the comparison criteria</param>
        /// <returns>Updated list of rollResults with additional dice for those that exploded</returns>
        private List<rollResult> ApplyExplodeModifier(List<rollResult> rolls, ExplodeModifier explodeModifier)
        {
            var result = new List<rollResult>();

            // Process each original roll
            foreach (var roll in rolls)
            {
                // Add the original roll to the result
                result.Add(roll);

                // Skip static dice types (constant, success)
                if (roll.type is DieType.Constant || roll.type is DieType.Success)
                {
                    continue;
                }

                // Check for explosion chain
                var currentRoll = roll;
                int explosionCounter = 0;

                while (explosionCounter < MaxExplosions &&
                      Compare(currentRoll.result, explodeModifier.Operator, explodeModifier.Value))
                {
                    // Roll another die of the same type
                    DieType newDieType = currentRoll.type switch
                    {
                        DieType.Basic basic => new DieType.Basic(basic.sides),
                        DieType.Percent => new DieType.Percent(),
                        DieType.Fudge => new DieType.Fudge(),
                        _ => currentRoll.type // Use existing type if not specifically handled
                    };

                    // Roll the new die
                    currentRoll = RollDie(newDieType);

                    // Add the new roll to the result
                    result.Add(currentRoll);

                    // Increment the counter to prevent infinite loops
                    explosionCounter++;
                }
            }

            return result;
        }

        /// <summary>
        /// Counts dice that meet the success criteria and returns a roll with the count as a Success type.
        /// </summary>
        /// <param name="rolls">The original roll results</param>
        /// <param name="successModifier">The success modifier containing the comparison criteria</param>
        /// <returns>A list with a single rollResult of type Success containing the count of successful dice</returns>
        private List<rollResult> ApplySuccessModifier(List<rollResult> rolls, SuccessModifier successModifier)
        {
            // Only count dynamic dice (not constants or existing success rolls)
            var successCount = rolls.Count(roll =>
                roll.type is not DieType.Constant &&
                roll.type is not DieType.Success &&
                Compare(roll.result, successModifier.Operator, successModifier.Value));

            // Return a single Success roll with the count of successes
            return new List<rollResult> { new rollResult(successCount, new DieType.Success()) };
        }

        /// <summary>
        /// Counts dice that meet the failure criteria and returns a roll with the negative count as a Success type.
        /// </summary>
        /// <param name="rolls">The original roll results</param>
        /// <param name="failureModifier">The failure modifier containing the comparison criteria</param>
        /// <returns>A list with a single rollResult of type Success containing the negative count of failed dice</returns>
        private List<rollResult> ApplyFailureModifier(List<rollResult> rolls, FailureModifier failureModifier)
        {
            // Only count dynamic dice (not constants or existing success rolls)

            var failureCount = rolls.Count(roll =>
                roll.type is not DieType.Constant &&
                roll.type is not DieType.Success &&
                Compare(roll.result, failureModifier.Operator, failureModifier.Value));

            // Return a single Success roll with the negative count of failures
            return new List<rollResult> { new rollResult(-failureCount, new DieType.Success()) };
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
                var maxRerolls = 10;
                
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
                    Success = evt.Success
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

        private void ApplyKeepOrDropModifierDetailed(List<DieEvent> events, int count, bool selectHighest, bool isKeep)
        {
            // Only process rollable events that aren't already discarded
            var rollableEvents = events
                .Where(e => e.Status != DieStatus.Discarded && ShouldProcessEvent(e))
                .ToList();

            if (isKeep && rollableEvents.Count <= count)
            {
                // Keep all if we have fewer than or equal to the keep count
                return;
            }

            var ordered = selectHighest
                ? rollableEvents.OrderByDescending(e => e.Value)
                : rollableEvents.OrderBy(e => e.Value);

            var selectedEvents = ordered.Take(count);

            if (isKeep)
            {
                // Use reference equality comparer to ensure we track specific DieEvent instances,
                // not value-based equality (since DieEvent is a record type)
                var keptEventSet = new HashSet<DieEvent>(selectedEvents, ReferenceEqualityComparer.Instance);

                // Mark non-kept rollable events as dropped
                foreach (var evt in rollableEvents)
                {
                    if (!keptEventSet.Contains(evt))
                    {
                        evt.Status = DieStatus.Dropped;
                    }
                }
            }
            else
            {
                // For drop, mark the selected events as dropped
                foreach (var evt in selectedEvents)
                {
                    evt.Status = DieStatus.Dropped;
                }
            }
        }

        private List<DieEvent> ApplySuccessModifierDetailed(List<DieEvent> events, SuccessModifier successModifier)
        {
            // Update success status for rollable events
            foreach (var evt in events)
            {
                if (evt.Status != DieStatus.Discarded && 
                    evt.Status != DieStatus.Dropped &&
                    ShouldProcessEvent(evt))
                {
                    if (Compare(evt.Value, successModifier.Operator, successModifier.Value))
                    {
                        evt.Success = SuccessStatus.Success;
                    }
                }
            }

            // Count successes and replace with a single success event
            var successCount = events.Count(e => e.Success == SuccessStatus.Success);
            
            return new List<DieEvent>
            {
                new DieEvent
                {
                    Value = successCount,
                    Type = DieEventType.Initial,
                    Significance = RollSignificance.None,
                    Status = DieStatus.Kept,
                    Success = SuccessStatus.Neutral
                }
            };
        }

        private List<DieEvent> ApplyFailureModifierDetailed(List<DieEvent> events, FailureModifier failureModifier)
        {
            // Update failure status for rollable events
            foreach (var evt in events)
            {
                if (evt.Status != DieStatus.Discarded && 
                    evt.Status != DieStatus.Dropped &&
                    ShouldProcessEvent(evt))
                {
                    if (Compare(evt.Value, failureModifier.Operator, failureModifier.Value))
                    {
                        evt.Success = SuccessStatus.Failure;
                    }
                }
            }

            // Count failures and return as negative
            var failureCount = events.Count(e => e.Success == SuccessStatus.Failure);
            
            return new List<DieEvent>
            {
                new DieEvent
                {
                    Value = -failureCount,
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
