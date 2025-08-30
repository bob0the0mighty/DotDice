using DotDice.Extension;
using DotDice.Evaluator;
using DotDice.Parser;
using DotDice.RandomNumberGenerator;

namespace DotDice.Tests
{
    [TestFixture]
    public class StringExtensionsDetailedTests
    {
        public class MockRandomNumberGenerator : IRandomNumberGenerator<int>
        {
            private readonly List<int> _numbers;
            private int _index = 0;

            public MockRandomNumberGenerator(List<int> numbers)
            {
                _numbers = numbers;
            }

            public int Next()
            {
                return _numbers[_index++];
            }

            public int Next(int maxValue)
            {
                return _numbers[_index++];
            }

            public int Next(int minValue, int maxValue)
            {
                return _numbers[_index++];
            }

            public void SetSeed(int seed)
            {
                throw new NotImplementedException();
            }

            public int GetSeed()
            {
                throw new NotImplementedException();
            }
        }

        [Test]
        public void ParseRollDetailed_BasicRoll_ReturnsCorrectResult()
        {
            // Arrange
            var rng = new MockRandomNumberGenerator(new List<int> { 4, 2 });

            // Act
            var result = "2d6".ParseRollDetailed(rng);

            // Assert
            Assert.AreEqual(6, result.Value);
            Assert.AreEqual(2, result.Events.Count);
            Assert.AreEqual(4, result.Events[0].Value);
            Assert.AreEqual(2, result.Events[1].Value);
            Assert.IsTrue(result.Events.All(e => e.Type == DieEventType.Initial));
            Assert.IsTrue(result.Events.All(e => e.Status == DieStatus.Kept));
        }

        [Test]
        public void ParseRollDetailed_WithModifiers_ReturnsCorrectResult()
        {
            // Arrange
            var rng = new MockRandomNumberGenerator(new List<int> { 1, 5, 3 });

            // Act - rolling 2d6, reroll values less than 2 (first die becomes 3)
            var result = "2d6ro<2".ParseRollDetailed(rng);

            // Assert
            Assert.AreEqual(8, result.Value); // 3 + 5
            Assert.AreEqual(3, result.Events.Count);
            
            // Original 1 (discarded)
            Assert.AreEqual(1, result.Events[0].Value);
            Assert.AreEqual(DieStatus.Discarded, result.Events[0].Status);
            
            // Original 5 (kept)
            Assert.AreEqual(5, result.Events[1].Value);
            Assert.AreEqual(DieStatus.Kept, result.Events[1].Status);
            
            // Reroll to 3
            Assert.AreEqual(3, result.Events[2].Value);
            Assert.AreEqual(DieEventType.Reroll, result.Events[2].Type);
        }

        [Test]
        public void ParseRollDetailed_ExplodingDice_ReturnsCorrectResult()
        {
            // Arrange
            var rng = new MockRandomNumberGenerator(new List<int> { 6, 4, 3 });

            // Act - 2d6, explode on 6
            var result = "2d6!=6".ParseRollDetailed(rng);

            // Assert
            Assert.AreEqual(13, result.Value); // 6 + 4 + 3
            Assert.AreEqual(3, result.Events.Count);
            
            // Original 6
            Assert.AreEqual(6, result.Events[0].Value);
            Assert.AreEqual(DieEventType.Initial, result.Events[0].Type);
            Assert.AreEqual(RollSignificance.Maximum, result.Events[0].Significance);
            
            // Original 4
            Assert.AreEqual(4, result.Events[1].Value);
            Assert.AreEqual(DieEventType.Initial, result.Events[1].Type);
            
            // Explosion from first die
            Assert.AreEqual(3, result.Events[2].Value);
            Assert.AreEqual(DieEventType.Explosion, result.Events[2].Type);
        }

        [Test]
        public void ParseRollDetailed_KeepHighest_ReturnsCorrectResult()
        {
            // Arrange
            var rng = new MockRandomNumberGenerator(new List<int> { 2, 6, 4 });

            // Act - 3d6, keep highest 2
            var result = "3d6kh2".ParseRollDetailed(rng);

            // Assert
            Assert.AreEqual(10, result.Value); // 6 + 4 (2 is dropped)
            Assert.AreEqual(3, result.Events.Count);
            
            // Check that the lowest die (2) is dropped
            var droppedEvent = result.Events.FirstOrDefault(e => e.Value == 2);
            Assert.IsNotNull(droppedEvent);
            Assert.AreEqual(DieStatus.Dropped, droppedEvent.Status);
            
            // Check that the highest dice are kept
            var keptEvents = result.Events.Where(e => e.Status == DieStatus.Kept).ToList();
            Assert.AreEqual(2, keptEvents.Count);
            Assert.IsTrue(keptEvents.Any(e => e.Value == 6));
            Assert.IsTrue(keptEvents.Any(e => e.Value == 4));
        }

        [Test]
        public void ParseRollDetailed_Constant_ReturnsCorrectResult()
        {
            // Act
            var result = "5".ParseRollDetailed();

            // Assert
            Assert.AreEqual(5, result.Value);
            Assert.AreEqual(0, result.Events.Count);
        }

        [Test]
        public void ParseRollDetailed_WithConstantModifier_ReturnsCorrectResult()
        {
            // Arrange
            var rng = new MockRandomNumberGenerator(new List<int> { 4 });

            // Act
            var result = "1d6+3".ParseRollDetailed(rng);

            // Assert
            Assert.AreEqual(7, result.Value);
            Assert.AreEqual(2, result.Events.Count);
            
            // Dice roll
            Assert.AreEqual(4, result.Events[0].Value);
            Assert.AreEqual(DieEventType.Initial, result.Events[0].Type);
            
            // Constant
            Assert.AreEqual(3, result.Events[1].Value);
            Assert.AreEqual(DieEventType.Initial, result.Events[1].Type);
        }

        [Test]
        public void ParseRollDetailed_InvalidFormat_ThrowsFormatException()
        {
            // Act & Assert
            Assert.Throws<FormatException>(() => "invalid".ParseRollDetailed());
        }
    }
}