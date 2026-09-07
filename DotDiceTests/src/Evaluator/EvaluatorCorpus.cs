using System.Collections.Generic;

namespace DotDice.Tests
{
    /// <summary>
    /// One expression plus the RNG script it is evaluated against.
    /// </summary>
    /// <param name="Expression">Dice notation, parsed with the real parser.</param>
    /// <param name="Script">
    /// Values the scripted RNG hands back, in order, cycling if the evaluation draws
    /// more than are listed.
    /// </param>
    public record EvaluatorCase(string Expression, int[] Script)
    {
        public override string ToString() => Expression;
    }

    /// <summary>
    /// A shared corpus of expression/script pairs covering every evaluator path.
    ///
    /// This exists so the behaviour-preserving tests around the evaluator all range
    /// over the same ground: value agreement between the simple and detailed paths,
    /// RNG draw order, and per-evaluation allocation. Adding an entry here extends
    /// all of them at once.
    ///
    /// Scripts are chosen so that generation-phase entries actually generate: the
    /// reroll cases really reroll, the explode cases really explode. Keep/drop cases
    /// contain duplicate values, since which of two equal dice gets marked Dropped is
    /// observable through EvaluateDetailed.
    /// </summary>
    public static class EvaluatorCorpus
    {
        public static IEnumerable<EvaluatorCase> Cases()
        {
            // Plain rolls, one per die type.
            yield return new EvaluatorCase("1d20", new[] { 7 });
            yield return new EvaluatorCase("3d6", new[] { 1, 2, 3 });
            yield return new EvaluatorCase("100d20", new[] { 3, 17, 20, 1, 11, 8 });
            yield return new EvaluatorCase("1d%", new[] { 42 });
            yield return new EvaluatorCase("4dF", new[] { -1, 0, 1, -1 });

            // Significance boundaries: minimum and maximum faces.
            yield return new EvaluatorCase("2d6", new[] { 1, 6 });
            yield return new EvaluatorCase("1d%", new[] { 100 });
            yield return new EvaluatorCase("2dF", new[] { -1, 1 });

            // Keep/drop, including duplicate values so tie-breaking is pinned.
            yield return new EvaluatorCase("4d6dl1", new[] { 6, 1, 3, 5 });
            yield return new EvaluatorCase("2d20kh1", new[] { 19, 4 });
            yield return new EvaluatorCase("4d6kh2", new[] { 5, 5, 5, 1 });
            yield return new EvaluatorCase("5d10kl2", new[] { 10, 2, 7, 2, 5 });
            yield return new EvaluatorCase("4d6dh2", new[] { 4, 4, 1, 6 });
            yield return new EvaluatorCase("6d6dl3", new[] { 2, 2, 2, 2, 5, 6 });
            // Selection count meeting or exceeding the pool: the degenerate branches.
            yield return new EvaluatorCase("2d6kh5", new[] { 3, 4 });
            yield return new EvaluatorCase("2d6dl2", new[] { 3, 4 });
            yield return new EvaluatorCase("2d6dl5", new[] { 3, 4 });

            // Rerolls.
            yield return new EvaluatorCase("4d6ro=1", new[] { 1, 3, 1, 6, 5, 2 });
            yield return new EvaluatorCase("4d6ro1", new[] { 1, 3, 1, 6, 5, 2 });
            yield return new EvaluatorCase("3d6rc<3", new[] { 1, 2, 5, 4, 6 });
            // Reroll-until that never satisfies its condition, to pin the MaxRerolls guard.
            yield return new EvaluatorCase("2d6rc<7", new[] { 4 });

            // Explosions, including the bare-"!" shorthand and the loop guard.
            yield return new EvaluatorCase("3d6!=6", new[] { 6, 2, 3, 6, 4 });
            yield return new EvaluatorCase("3d6!", new[] { 6, 2, 3, 4 });
            yield return new EvaluatorCase("10d6!", new[] { 6, 1, 2, 3, 4, 5 });
            yield return new EvaluatorCase("1d6!<7", new[] { 4 });

            // Compounds, including the "!!" alias and the loop guard.
            yield return new EvaluatorCase("3d6^=6", new[] { 6, 2, 3, 4 });
            yield return new EvaluatorCase("3d6!!", new[] { 6, 6, 2, 3, 4 });
            yield return new EvaluatorCase("1d6^<7", new[] { 4 });

            // Success and failure counting, separately and together.
            yield return new EvaluatorCase("5d6>4", new[] { 5, 6, 1, 2, 3 });
            yield return new EvaluatorCase("5d6f<3", new[] { 5, 6, 1, 2, 3 });
            yield return new EvaluatorCase("6d10>8f<2", new[] { 9, 1, 10, 5, 2, 1 });
            yield return new EvaluatorCase("4d6>3f<2", new[] { 6, 1, 4, 2 });

            // Success counting downstream of the phases that precede it.
            yield return new EvaluatorCase("6d10kh3>7", new[] { 9, 1, 10, 5, 2, 8 });
            yield return new EvaluatorCase("4d6!>4", new[] { 6, 2, 5, 1, 3 });

            // Constants and arithmetic.
            yield return new EvaluatorCase("2d6+3", new[] { 4, 5 });
            yield return new EvaluatorCase("2d6-1", new[] { 4, 5 });
            yield return new EvaluatorCase("3d20-2d4", new[] { 10, 15, 3, 2, 4 });
            yield return new EvaluatorCase("1d20+5", new[] { 11 });
            yield return new EvaluatorCase("2d6+1d8+2", new[] { 3, 4, 7 });
            yield return new EvaluatorCase("4d6dl1+2d8kh1-3", new[] { 6, 1, 3, 5, 2, 8 });

            // Modifier combinations that cross phase boundaries.
            yield return new EvaluatorCase("4d6ro=1kh2", new[] { 1, 3, 6, 5, 4 });
            yield return new EvaluatorCase("6d6!kh3", new[] { 6, 2, 3, 6, 1, 5, 4 });
        }
    }
}
