using DotDice.Evaluator;
using DotDice.Parser;
using DotDice.RandomNumberGenerator;

namespace DotDice.Tests
{
    [TestFixture]
    public class DiceEvaluationResultTests
    {

        [Test]
        public void EvaluateDetailed_BasicRoll_CreatesCorrectEvents()
        {
            // Arrange
            var evaluator = new DiceEvaluator(new TestHelpers.MockRandomNumberGenerator(new List<int> { 3, 5 }));
            var basicRoll = new BasicRoll(2, new DieType.Basic(6), new List<Modifier>());

            // Act
            var result = evaluator.EvaluateDetailed(basicRoll);

            // Assert
            Assert.That(result.Value, Is.EqualTo(8));
            Assert.That(result.Events.Count, Is.EqualTo(2));
            
            Assert.That(result.Events[0].Value, Is.EqualTo(3));
            Assert.That(result.Events[0].Type, Is.EqualTo(DieEventType.Initial));
            Assert.That(result.Events[0].Status, Is.EqualTo(DieStatus.Kept));
            Assert.That(result.Events[0].Significance, Is.EqualTo(RollSignificance.None));
            
            Assert.That(result.Events[1].Value, Is.EqualTo(5));
            Assert.That(result.Events[1].Type, Is.EqualTo(DieEventType.Initial));
            Assert.That(result.Events[1].Status, Is.EqualTo(DieStatus.Kept));
            Assert.That(result.Events[1].Significance, Is.EqualTo(RollSignificance.None));
        }

        [Test]
        public void EvaluateDetailed_BasicRollWithMinMax_SetsSignificanceCorrectly()
        {
            // Arrange
            var evaluator = new DiceEvaluator(new TestHelpers.MockRandomNumberGenerator(new List<int> { 1, 6 }));
            var basicRoll = new BasicRoll(2, new DieType.Basic(6), new List<Modifier>());

            // Act
            var result = evaluator.EvaluateDetailed(basicRoll);

            // Assert
            Assert.That(result.Value, Is.EqualTo(7));
            Assert.That(result.Events.Count, Is.EqualTo(2));
            
            Assert.That(result.Events[0].Value, Is.EqualTo(1));
            Assert.That(result.Events[0].Significance, Is.EqualTo(RollSignificance.Minimum));
            
            Assert.That(result.Events[1].Value, Is.EqualTo(6));
            Assert.That(result.Events[1].Significance, Is.EqualTo(RollSignificance.Maximum));
        }

        [Test]
        public void EvaluateDetailed_WithKeepModifier_UpdatesStatusCorrectly()
        {
            // Arrange
            var evaluator = new DiceEvaluator(new TestHelpers.MockRandomNumberGenerator(new List<int> { 2, 4, 6 }));
            var modifiers = new List<Modifier> { new KeepModifier(2, true) }; // Keep highest 2
            var basicRoll = new BasicRoll(3, new DieType.Basic(6), modifiers);

            // Act
            var result = evaluator.EvaluateDetailed(basicRoll);

            // Assert
            Assert.That(result.Value, Is.EqualTo(10)); // 4 + 6
            Assert.That(result.Events.Count, Is.EqualTo(3));
            
            // The lowest die (2) should be dropped
            var droppedEvent = result.Events.FirstOrDefault(e => e.Value == 2);
            Assert.IsNotNull(droppedEvent);
            Assert.That(droppedEvent.Status, Is.EqualTo(DieStatus.Dropped));
            
            // The highest dice (4, 6) should be kept
            var keptEvents = result.Events.Where(e => e.Status == DieStatus.Kept).ToList();
            Assert.That(keptEvents.Count, Is.EqualTo(2));
            Assert.IsTrue(keptEvents.Any(e => e.Value == 4));
            Assert.IsTrue(keptEvents.Any(e => e.Value == 6));
        }

        [Test]
        public void EvaluateDetailed_WithRerollOnce_CreatesRerollEvents()
        {
            // Arrange: First die rolls 1 (should reroll to 4), second die rolls 3 (no reroll)
            var evaluator = new DiceEvaluator(new TestHelpers.MockRandomNumberGenerator(new List<int> { 1, 3, 4 }));
            var modifiers = new List<Modifier> { new RerollOnceModifier(ComparisonOperator.Equal, 1) };
            var basicRoll = new BasicRoll(2, new DieType.Basic(6), modifiers);

            // Act
            var result = evaluator.EvaluateDetailed(basicRoll);

            // Assert
            Assert.That(result.Value, Is.EqualTo(7)); // 4 + 3
            Assert.That(result.Events.Count, Is.EqualTo(3));
            
            // First event should be the original 1, now discarded
            Assert.That(result.Events[0].Value, Is.EqualTo(1));
            Assert.That(result.Events[0].Type, Is.EqualTo(DieEventType.Initial));
            Assert.That(result.Events[0].Status, Is.EqualTo(DieStatus.Discarded));
            
            // Second event should be the 3, kept
            Assert.That(result.Events[1].Value, Is.EqualTo(3));
            Assert.That(result.Events[1].Type, Is.EqualTo(DieEventType.Initial));
            Assert.That(result.Events[1].Status, Is.EqualTo(DieStatus.Kept));
            
            // Third event should be the reroll to 4
            Assert.That(result.Events[2].Value, Is.EqualTo(4));
            Assert.That(result.Events[2].Type, Is.EqualTo(DieEventType.Reroll));
            Assert.That(result.Events[2].Status, Is.EqualTo(DieStatus.Kept));
        }

        [Test]
        public void EvaluateDetailed_WithExplodingDice_CreatesExplosionEvents()
        {
            // Arrange: Roll 6 (explodes to 5), 3 (no explosion)
            var evaluator = new DiceEvaluator(new TestHelpers.MockRandomNumberGenerator(new List<int> { 6, 3, 5 }));
            var modifiers = new List<Modifier> { new ExplodeModifier(ComparisonOperator.Equal, 6) };
            var basicRoll = new BasicRoll(2, new DieType.Basic(6), modifiers);

            // Act
            var result = evaluator.EvaluateDetailed(basicRoll);

            // Assert
            Assert.That(result.Value, Is.EqualTo(14)); // 6 + 3 + 5
            Assert.That(result.Events.Count, Is.EqualTo(3));
            
            // First event: original 6
            Assert.That(result.Events[0].Value, Is.EqualTo(6));
            Assert.That(result.Events[0].Type, Is.EqualTo(DieEventType.Initial));
            Assert.That(result.Events[0].Status, Is.EqualTo(DieStatus.Kept));
            
            // Second event: original 3
            Assert.That(result.Events[1].Value, Is.EqualTo(3));
            Assert.That(result.Events[1].Type, Is.EqualTo(DieEventType.Initial));
            Assert.That(result.Events[1].Status, Is.EqualTo(DieStatus.Kept));
            
            // Third event: explosion from the 6
            Assert.That(result.Events[2].Value, Is.EqualTo(5));
            Assert.That(result.Events[2].Type, Is.EqualTo(DieEventType.Explosion));
            Assert.That(result.Events[2].Status, Is.EqualTo(DieStatus.Kept));
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
            Assert.That(result.Value, Is.EqualTo(5));
            Assert.That(result.Events.Count, Is.EqualTo(0));
        }

        [Test]
        public void EvaluateDetailed_WithConstantModifier_AddsConstantEvent()
        {
            // Arrange
            var evaluator = new DiceEvaluator(new TestHelpers.MockRandomNumberGenerator(new List<int> { 4 }));
            var modifiers = new List<Modifier> { new ConstantModifier(ArithmeticOperator.Add, 3) };
            var basicRoll = new BasicRoll(1, new DieType.Basic(6), modifiers);

            // Act
            var result = evaluator.EvaluateDetailed(basicRoll);

            // Assert
            Assert.That(result.Value, Is.EqualTo(7)); // 4 + 3
            Assert.That(result.Events.Count, Is.EqualTo(2));
            
            // First event: the die roll
            Assert.That(result.Events[0].Value, Is.EqualTo(4));
            Assert.That(result.Events[0].Type, Is.EqualTo(DieEventType.Initial));
            
            // Second event: the constant
            Assert.That(result.Events[1].Value, Is.EqualTo(3));
            Assert.That(result.Events[1].Type, Is.EqualTo(DieEventType.Initial));
            Assert.That(result.Events[1].Status, Is.EqualTo(DieStatus.Kept));
        }
    }
}