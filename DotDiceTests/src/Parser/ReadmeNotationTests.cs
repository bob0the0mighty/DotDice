using System.Text.RegularExpressions;
using DotDice.Parser;
using Pidgin;

namespace DotDice.Tests
{
    /// <summary>
    /// Parses every notation the README advertises.
    ///
    /// The README shipped with 1.5.0 documented `r`, `rr`, `cs` and `cf`, none of which
    /// the parser has ever accepted. Nothing caught it because the notation reference was
    /// prose. This reads that reference out of the file and runs it through the parser, so
    /// documenting syntax the grammar does not have is a build failure rather than a
    /// support question.
    /// </summary>
    [TestFixture]
    public class ReadmeNotationTests
    {
        /// <summary>
        /// Sections of the notation reference whose bullets name real expressions. The
        /// bullets elsewhere in the README are prose or C# snippets.
        /// </summary>
        private static readonly string[] NotationSections =
        {
            "### Basic Dice",
            "### Arithmetic Expressions",
            "### Modifiers",
            "### Examples"
        };

        /// <summary>
        /// A token that already opens with a die specification is a whole expression;
        /// anything else is a modifier fragment that needs dice in front of it.
        /// </summary>
        private static readonly Regex DieSpecification = new(@"^\d*d(\d+|%|F)", RegexOptions.Compiled);

        private static readonly Regex CodeSpan = new(@"`([^`]+)`", RegexOptions.Compiled);

        [TestCaseSource(nameof(DocumentedNotations))]
        public void EveryDocumentedNotationParses(string token, string expression)
        {
            var result = DiceParser.Roll.Parse(expression);

            Assert.That(
                result.Success,
                Is.True,
                $"The README documents `{token}`, which the parser rejects as \"{expression}\". {(result.Success ? string.Empty : result.Error?.ToString())}");
        }

        [Test]
        public void NotationReferenceIsFound()
        {
            // Guards the harvester itself: a README rename or a heading change would
            // otherwise turn the test above into an empty, permanently passing suite.
            Assert.That(DocumentedNotations().Count, Is.GreaterThanOrEqualTo(20));
        }

        private static List<TestCaseData> DocumentedNotations()
        {
            var cases = new List<TestCaseData>();
            var inNotationSection = false;

            foreach (var line in File.ReadLines(ReadmePath()))
            {
                if (line.StartsWith("## ", StringComparison.Ordinal))
                {
                    inNotationSection = false;
                    continue;
                }

                if (line.StartsWith("### ", StringComparison.Ordinal))
                {
                    inNotationSection = NotationSections.Contains(line.Trim());
                    continue;
                }

                if (!inNotationSection || !line.StartsWith("- ", StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (var token in TokensIn(line))
                {
                    cases.Add(new TestCaseData(token, ToExpression(token)).SetArgDisplayNames(token));
                }
            }

            return cases;
        }

        /// <summary>
        /// The notations a bullet names, which is everything in code spans up to the
        /// " - " that introduces the description. The description mentions property names
        /// and prose examples that are not notations.
        /// </summary>
        private static IEnumerable<string> TokensIn(string bullet)
        {
            var body = bullet.Substring(2);
            var descriptionStart = body.IndexOf(" - ", StringComparison.Ordinal);
            var head = descriptionStart >= 0 ? body.Substring(0, descriptionStart) : body;

            return CodeSpan.Matches(head)
                .Select(match => match.Groups[1].Value)
                // The arithmetic operators are documented on their own, and are only
                // expressions in combination with the terms around them.
                .Where(token => token != "+" && token != "-");
        }

        private static string ToExpression(string token)
        {
            // "#" is the README's placeholder for a number.
            var concrete = token.Replace("#", "3");
            return DieSpecification.IsMatch(concrete) ? concrete : "4d6" + concrete;
        }

        private static string ReadmePath()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "dotDice.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Could not locate the repository root from the test directory.");
            return Path.Combine(directory!.FullName, "README.md");
        }
    }
}
