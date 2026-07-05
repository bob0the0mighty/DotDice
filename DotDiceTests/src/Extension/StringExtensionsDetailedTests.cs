using DotDice.Extension;
using DotDice.Evaluator;
using DotDice.Parser;
using DotDice.RandomNumberGenerator;

namespace DotDice.Tests
{
    [TestFixture]
    public class StringExtensionsDetailedTests
    {


        [Test]
        public void ParseRollDetailed_BasicRoll_ReturnsCorrectResult()
        {
            // Arrange
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 4, 2 });

            // Act
            var result = "2d6".ParseRollDetailed(rng);

            // Assert
            Assert.That(result.Value, Is.EqualTo(6));
            Assert.That(result.Events.Count, Is.EqualTo(2));
            Assert.That(result.Events[0].Value, Is.EqualTo(4));
            Assert.That(result.Events[1].Value, Is.EqualTo(2));
            Assert.IsTrue(result.Events.All(e => e.Type == DieEventType.Initial));
            Assert.IsTrue(result.Events.All(e => e.Status == DieStatus.Kept));
        }

        [Test]
        public void ParseRollDetailed_WithModifiers_ReturnsCorrectResult()
        {
            // Arrange
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 1, 5, 3 });

            // Act - rolling 2d6, reroll values less than 2 (first die becomes 3)
            var result = "2d6ro<2".ParseRollDetailed(rng);

            // Assert
            Assert.That(result.Value, Is.EqualTo(8)); // 3 + 5
            Assert.That(result.Events.Count, Is.EqualTo(3));
            
            // Original 1 (discarded)
            Assert.That(result.Events[0].Value, Is.EqualTo(1));
            Assert.That(result.Events[0].Status, Is.EqualTo(DieStatus.Discarded));
            
            // Original 5 (kept)
            Assert.That(result.Events[1].Value, Is.EqualTo(5));
            Assert.That(result.Events[1].Status, Is.EqualTo(DieStatus.Kept));
            
            // Reroll to 3
            Assert.That(result.Events[2].Value, Is.EqualTo(3));
            Assert.That(result.Events[2].Type, Is.EqualTo(DieEventType.Reroll));
        }

        [Test]
        public void ParseRollDetailed_ExplodingDice_ReturnsCorrectResult()
        {
            // Arrange
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 6, 4, 3 });

            // Act - 2d6, explode on 6
            var result = "2d6!=6".ParseRollDetailed(rng);

            // Assert
            Assert.That(result.Value, Is.EqualTo(13)); // 6 + 4 + 3
            Assert.That(result.Events.Count, Is.EqualTo(3));
            
            // Original 6
            Assert.That(result.Events[0].Value, Is.EqualTo(6));
            Assert.That(result.Events[0].Type, Is.EqualTo(DieEventType.Initial));
            Assert.That(result.Events[0].Significance, Is.EqualTo(RollSignificance.Maximum));
            
            // Original 4
            Assert.That(result.Events[1].Value, Is.EqualTo(4));
            Assert.That(result.Events[1].Type, Is.EqualTo(DieEventType.Initial));
            
            // Explosion from first die
            Assert.That(result.Events[2].Value, Is.EqualTo(3));
            Assert.That(result.Events[2].Type, Is.EqualTo(DieEventType.Explosion));
        }

        [Test]
        public void ParseRollDetailed_KeepHighest_ReturnsCorrectResult()
        {
            // Arrange
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 2, 6, 4 });

            // Act - 3d6, keep highest 2
            var result = "3d6kh2".ParseRollDetailed(rng);

            // Assert
            Assert.That(result.Value, Is.EqualTo(10)); // 6 + 4 (2 is dropped)
            Assert.That(result.Events.Count, Is.EqualTo(3));
            
            // Check that the lowest die (2) is dropped
            var droppedEvent = result.Events.FirstOrDefault(e => e.Value == 2);
            Assert.IsNotNull(droppedEvent);
            Assert.That(droppedEvent.Status, Is.EqualTo(DieStatus.Dropped));
            
            // Check that the highest dice are kept
            var keptEvents = result.Events.Where(e => e.Status == DieStatus.Kept).ToList();
            Assert.That(keptEvents.Count, Is.EqualTo(2));
            Assert.IsTrue(keptEvents.Any(e => e.Value == 6));
            Assert.IsTrue(keptEvents.Any(e => e.Value == 4));
        }

        [Test]
        public void ParseRollDetailed_Constant_ReturnsCorrectResult()
        {
            // Act
            var result = "5".ParseRollDetailed();

            // Assert
            Assert.That(result.Value, Is.EqualTo(5));
            Assert.That(result.Events.Count, Is.EqualTo(0));
        }

        [Test]
        public void ParseRollDetailed_WithConstantModifier_ReturnsCorrectResult()
        {
            // Arrange
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 4 });

            // Act
            var result = "1d6+3".ParseRollDetailed(rng);

            // Assert
            Assert.That(result.Value, Is.EqualTo(7));
            Assert.That(result.Events.Count, Is.EqualTo(2));
            
            // Dice roll
            Assert.That(result.Events[0].Value, Is.EqualTo(4));
            Assert.That(result.Events[0].Type, Is.EqualTo(DieEventType.Initial));
            
            // Constant
            Assert.That(result.Events[1].Value, Is.EqualTo(3));
            Assert.That(result.Events[1].Type, Is.EqualTo(DieEventType.Initial));
        }

        [Test]
        public void ParseRollDetailed_InvalidFormat_ThrowsFormatException()
        {
            // Act & Assert
            Assert.Throws<FormatException>(() => "invalid".ParseRollDetailed());
        }
    }
}