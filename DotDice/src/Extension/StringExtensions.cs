using DotDice.Parser;
using DotDice.Evaluator;
using DotDice.RandomNumberGenerator;
using Pidgin;


namespace DotDice.Extension
{
    public static class StringExtensions
    {
        /// <summary>
        /// Adds the ability to get roll results from a string.
        /// </summary>
        /// <param name="input">The string to parse.</param>
        /// <param name="randomNumberGenerator">Optional random number generator to use for evaluation.</param>
        /// <returns>The roll results.</returns>
        /// <exception cref="FormatException">Thrown when the input is not a valid roll.</exception>
        public static int ParseRoll(this string input, IRandomNumberGenerator<int>? randomNumberGenerator = null)
        {
            var parser = DiceParser.Roll.Parse(input);
            if (parser is null || !parser.Success)
            {
                throw new FormatException("Invalid roll format.");
            }

            var evaluator = randomNumberGenerator != null 
                ? new DiceEvaluator(randomNumberGenerator)
                : new DiceEvaluator();
            
            return evaluator.Evaluate(parser.Value);
        }

        /// <summary>
        /// Adds the ability to get detailed roll results with individual die events from a string.
        /// </summary>
        /// <param name="input">The string to parse.</param>
        /// <param name="randomNumberGenerator">Optional random number generator to use for evaluation.</param>
        /// <returns>The detailed roll results including individual die events.</returns>
        /// <exception cref="FormatException">Thrown when the input is not a valid roll.</exception>
        public static DiceEvaluationResult ParseRollDetailed(this string input, IRandomNumberGenerator<int>? randomNumberGenerator = null)
        {
            var parser = DiceParser.Roll.Parse(input);
            if (parser is null || !parser.Success)
            {
                throw new FormatException("Invalid roll format.");
            }

            var evaluator = randomNumberGenerator != null 
                ? new DiceEvaluator(randomNumberGenerator)
                : new DiceEvaluator();
            
            return evaluator.EvaluateDetailed(parser.Value);
        }
    }
}