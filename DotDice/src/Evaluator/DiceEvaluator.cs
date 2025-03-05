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
                default:
                    throw new ArgumentException("Unknown roll type", nameof(roll));
            }
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

        private rollResult RollDie(DieType dieType)
        {
            return dieType switch
            {
                DieType.Basic roll => new (_rng.Next(1, roll.sides + 1), dieType),
                DieType.Reroll roll => new (_rng.Next(1, roll.sides + 1), dieType),
                DieType.Percent => new (_rng.Next(1, 101), dieType),
                DieType.Fudge => new (_rng.Next(-1, 2), dieType),
                _ => throw new ArgumentException("Unknown die type", nameof(dieType))
            };
        }

        private List<rollResult> ApplyModifiers(List<rollResult> rolls, IEnumerable<Modifier> modifiers)
        {
            // Placeholder for modifier application logic
            // This will need to be expanded based on the specific modifiers
            // For example, KeepModifier, DropModifier, etc.
            foreach (var modifier in modifiers)
            {
                switch (modifier)
                {
                    case ConstantModifier constantModifier:
                        rolls = ApplyConstantModifier(rolls, constantModifier);
                        break;
                    case DropModifier dropModifier:
                        rolls = ApplyDropModifier(rolls, dropModifier);
                        break;
                    case KeepModifier keepModifier:
                        rolls = ApplyKeepModifier(rolls, keepModifier);
                        break;
                }
            }

            return rolls;
        }

        private List<rollResult> ApplyKeepModifier(List<rollResult> rolls, KeepModifier keepModifier)
        {
            if (keepModifier.KeepHighest)
            {
                return rolls.OrderByDescending(x => x.result)
                    .Take(keepModifier.Count)
                    .ToList();
            }
            else
            {
                return rolls.OrderBy(x => x.result)
                    .Take(keepModifier.Count)
                    .ToList();
            }
        }

        private List<rollResult> ApplyDropModifier(List<rollResult> rolls, DropModifier dropModifier)
        {
            if (dropModifier.DropHighest)
            {
                return rolls.OrderByDescending(x => x.result)
                    .Skip(dropModifier.Count)
                    .ToList();
            }
            else
            {
                return rolls.OrderBy(x => x.result)
                    .Skip(dropModifier.Count)
                    .ToList();
            }
        }

        private List<rollResult> ApplyConstantModifier(List<rollResult> rolls, ConstantModifier constantModifier)
        {
            //Constant should be applied after everything else, and just add a value to the total of all rolls, not individual rolls
            var value = constantModifier.Operator switch
            {
                ArithmaticOperator.Add => constantModifier.Value,
                ArithmaticOperator.Subtract => -constantModifier.Value,
                _ => throw new InvalidEnumArgumentException("Invalid ArithmaticOperator")
            };
            return rolls.Append(new (value, new DieType.Constant()))
                .ToList();
        }

        /// <summary>
        /// Reroll once modifier will reroll any dice that meets the condition once.
        /// </summary>
        /// <param name="rolls"></param>
        /// <param name="rerollOnceModifier"></param>
        /// <returns></returns>
        private List<rollResult> ApplyRerollOnceModifier(List<rollResult> rolls, RerollOnceModifier rerollOnceModifier)
        {
            return rolls.Select(roll => {
            
                var comparison = rerollOnceModifier.Operator switch
                {
                    ComparisonOperator.GreaterThan => roll.result > rerollOnceModifier.Value,
                    ComparisonOperator.LessThan => roll.result < rerollOnceModifier.Value,
                    ComparisonOperator.Equal => roll.result == rerollOnceModifier.Value,
                    _ => throw new InvalidEnumArgumentException("Invalid ComparisonOperator")
                };
                return comparison ? RollDie(new DieType.Reroll(roll.result)) : roll;
            })
            .ToList();
        }
    }
}
