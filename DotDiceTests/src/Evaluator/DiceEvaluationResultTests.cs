using DotDice.Evaluator;
using DotDice.Parser;
using DotDice.RandomNumberGenerator;

namespace DotDice.Tests
{
    [TestFixture]
    public class DiceEvaluationResultTests
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
        public void EvaluateDetailed_BasicRoll_CreatesCorrectEvents()
        {
            // Arrange
            var evaluator = new DiceEvaluator(new MockRandomNumberGenerator(new List<int> { 3, 5 }));
            var basicRoll = new BasicRoll(2, new DieType.Basic(6), new List<Modifier>());

            // Act
            var result = evaluator.EvaluateDetailed(basicRoll);

            // Assert
            Assert.AreEqual(8, result.Value);
            Assert.AreEqual(2, result.Events.Count);
            
            Assert.AreEqual(3, result.Events[0].Value);
            Assert.AreEqual(DieEventType.Initial, result.Events[0].Type);
            Assert.AreEqual(DieStatus.Kept, result.Events[0].Status);
            Assert.AreEqual(RollSignificance.None, result.Events[0].Significance);
            
            Assert.AreEqual(5, result.Events[1].Value);
            Assert.AreEqual(DieEventType.Initial, result.Events[1].Type);
            Assert.AreEqual(DieStatus.Kept, result.Events[1].Status);
            Assert.AreEqual(RollSignificance.None, result.Events[1].Significance);
        }

        [Test]
        public void EvaluateDetailed_BasicRollWithMinMax_SetsSignificanceCorrectly()
        {
            // Arrange
            var evaluator = new DiceEvaluator(new MockRandomNumberGenerator(new List<int> { 1, 6 }));
            var basicRoll = new BasicRoll(2, new DieType.Basic(6), new List<Modifier>());

            // Act
            var result = evaluator.EvaluateDetailed(basicRoll);

            // Assert
            Assert.AreEqual(7, result.Value);
            Assert.AreEqual(2, result.Events.Count);
            
            Assert.AreEqual(1, result.Events[0].Value);
            Assert.AreEqual(RollSignificance.Minimum, result.Events[0].Significance);
            
            Assert.AreEqual(6, result.Events[1].Value);
            Assert.AreEqual(RollSignificance.Maximum, result.Events[1].Significance);
        }

        [Test]
        public void EvaluateDetailed_WithKeepModifier_UpdatesStatusCorrectly()
        {
            // Arrange
            var evaluator = new DiceEvaluator(new MockRandomNumberGenerator(new List<int> { 2, 4, 6 }));
            var modifiers = new List<Modifier> { new KeepModifier(2, true) }; // Keep highest 2
            var basicRoll = new BasicRoll(3, new DieType.Basic(6), modifiers);

            // Act
            var result = evaluator.EvaluateDetailed(basicRoll);

            // Assert
            Assert.AreEqual(10, result.Value); // 4 + 6
            Assert.AreEqual(3, result.Events.Count);
            
            // The lowest die (2) should be dropped
            var droppedEvent = result.Events.FirstOrDefault(e => e.Value == 2);
            Assert.IsNotNull(droppedEvent);
            Assert.AreEqual(DieStatus.Dropped, droppedEvent.Status);
            
            // The highest dice (4, 6) should be kept
            var keptEvents = result.Events.Where(e => e.Status == DieStatus.Kept).ToList();
            Assert.AreEqual(2, keptEvents.Count);
            Assert.IsTrue(keptEvents.Any(e => e.Value == 4));
            Assert.IsTrue(keptEvents.Any(e => e.Value == 6));
        }

        [Test]
        public void EvaluateDetailed_WithRerollOnce_CreatesRerollEvents()
        {
            // Arrange: First die rolls 1 (should reroll to 4), second die rolls 3 (no reroll)
            var evaluator = new DiceEvaluator(new MockRandomNumberGenerator(new List<int> { 1, 3, 4 }));
            var modifiers = new List<Modifier> { new RerollOnceModifier(ComparisonOperator.Equal, 1) };
            var basicRoll = new BasicRoll(2, new DieType.Basic(6), modifiers);

            // Act
            var result = evaluator.EvaluateDetailed(basicRoll);

            // Assert
            Assert.AreEqual(7, result.Value); // 4 + 3
            Assert.AreEqual(3, result.Events.Count);
            
            // First event should be the original 1, now discarded
            Assert.AreEqual(1, result.Events[0].Value);
            Assert.AreEqual(DieEventType.Initial, result.Events[0].Type);
            Assert.AreEqual(DieStatus.Discarded, result.Events[0].Status);
            
            // Second event should be the 3, kept
            Assert.AreEqual(3, result.Events[1].Value);
            Assert.AreEqual(DieEventType.Initial, result.Events[1].Type);
            Assert.AreEqual(DieStatus.Kept, result.Events[1].Status);
            
            // Third event should be the reroll to 4
            Assert.AreEqual(4, result.Events[2].Value);
            Assert.AreEqual(DieEventType.Reroll, result.Events[2].Type);
            Assert.AreEqual(DieStatus.Kept, result.Events[2].Status);
        }

        [Test]
        public void EvaluateDetailed_WithExplodingDice_CreatesExplosionEvents()
        {
            // Arrange: Roll 6 (explodes to 5), 3 (no explosion)
            var evaluator = new DiceEvaluator(new MockRandomNumberGenerator(new List<int> { 6, 3, 5 }));
            var modifiers = new List<Modifier> { new ExplodeModifier(ComparisonOperator.Equal, 6) };
            var basicRoll = new BasicRoll(2, new DieType.Basic(6), modifiers);

            // Act
            var result = evaluator.EvaluateDetailed(basicRoll);

            // Assert
            Assert.AreEqual(14, result.Value); // 6 + 3 + 5
            Assert.AreEqual(3, result.Events.Count);
            
            // First event: original 6
            Assert.AreEqual(6, result.Events[0].Value);
            Assert.AreEqual(DieEventType.Initial, result.Events[0].Type);
            Assert.AreEqual(DieStatus.Kept, result.Events[0].Status);
            
            // Second event: original 3
            Assert.AreEqual(3, result.Events[1].Value);
            Assert.AreEqual(DieEventType.Initial, result.Events[1].Type);
            Assert.AreEqual(DieStatus.Kept, result.Events[1].Status);
            
            // Third event: explosion from the 6
            Assert.AreEqual(5, result.Events[2].Value);
            Assert.AreEqual(DieEventType.Explosion, result.Events[2].Type);
            Assert.AreEqual(DieStatus.Kept, result.Events[2].Status);
        }

        [Test]
        public void EvaluateDetailed_Constant_ReturnsCorrectResult()
        {
            // Arrange
            var evaluator = new DiceEvaluator();
            var constant = new Constant(5);

            // Act
            var result = evaluator.EvaluateDetailed(constant);

            // Assert
            Assert.AreEqual(5, result.Value);
            Assert.AreEqual(0, result.Events.Count);
        }

        [Test]
        public void EvaluateDetailed_WithConstantModifier_AddsConstantEvent()
        {
            // Arrange
            var evaluator = new DiceEvaluator(new MockRandomNumberGenerator(new List<int> { 4 }));
            var modifiers = new List<Modifier> { new ConstantModifier(ArithmeticOperator.Add, 3) };
            var basicRoll = new BasicRoll(1, new DieType.Basic(6), modifiers);

            // Act
            var result = evaluator.EvaluateDetailed(basicRoll);

            // Assert
            Assert.AreEqual(7, result.Value); // 4 + 3
            Assert.AreEqual(2, result.Events.Count);
            
            // First event: the die roll
            Assert.AreEqual(4, result.Events[0].Value);
            Assert.AreEqual(DieEventType.Initial, result.Events[0].Type);
            
            // Second event: the constant
            Assert.AreEqual(3, result.Events[1].Value);
            Assert.AreEqual(DieEventType.Initial, result.Events[1].Type);
            Assert.AreEqual(DieStatus.Kept, result.Events[1].Status);
        }
    }
}