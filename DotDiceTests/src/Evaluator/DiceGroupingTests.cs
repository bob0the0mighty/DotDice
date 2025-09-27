using DotDice.Extension;
using DotDice.Evaluator;
using DotDice.Parser;
using DotDice.RandomNumberGenerator;

namespace DotDice.Tests
{
    [TestFixture]
    public class DiceGroupingTests
    {
        [Test]
        public void ParseRollDetailed_SimpleArithmetic_AssignsCorrectGroupInformation()
        {
            // Arrange
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 4, 2, 6, 1, 3, 2, 1 });

            // Act
            var result = "3d20-4d4".ParseRollDetailed(rng);

            // Assert
            Assert.That(result.Value, Is.EqualTo(5)); // (4+2+6) - (1+3+2+1) = 12 - 7 = 5
            Assert.That(result.Events.Count, Is.EqualTo(7));
            
            // Check group 0 (3d20 - Addition)
            var group0Events = result.Events.Where(e => e.GroupId == 0).ToList();
            Assert.That(group0Events.Count, Is.EqualTo(3));
            Assert.IsTrue(group0Events.All(e => e.GroupOperator == ArithmeticOperator.Add));
            Assert.IsTrue(group0Events.All(e => e.DieType is DieType.Basic && ((DieType.Basic)e.DieType).sides == 20));
            Assert.That(group0Events.Select(e => e.Value), Is.EquivalentTo(new[] { 4, 2, 6 }));
            
            // Check group 1 (4d4 - Subtraction)
            var group1Events = result.Events.Where(e => e.GroupId == 1).ToList();
            Assert.That(group1Events.Count, Is.EqualTo(4));
            Assert.IsTrue(group1Events.All(e => e.GroupOperator == ArithmeticOperator.Subtract));
            Assert.IsTrue(group1Events.All(e => e.DieType is DieType.Basic && ((DieType.Basic)e.DieType).sides == 4));
            Assert.That(group1Events.Select(e => e.Value), Is.EquivalentTo(new[] { 1, 3, 2, 1 }));
        }

        [Test]
        public void ParseRollDetailed_ComplexArithmeticWithModifiers_GroupsCorrectly()
        {
            // Arrange - 3d20kh1-4d4+5: keep highest of 3d20, subtract 4d4, add constant 5
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 5, 15, 8, 2, 4, 1, 3 });

            // Act
            var result = "3d20kh1-4d4+5".ParseRollDetailed(rng);

            // Assert
            Assert.That(result.Value, Is.EqualTo(10)); // 15 - (2+4+1+3) + 5 = 15 - 10 + 5 = 10
            
            // Check group 0 (3d20kh1 - Addition) - should have 3 events, 2 dropped, 1 kept
            var group0Events = result.Events.Where(e => e.GroupId == 0).ToList();
            Assert.That(group0Events.Count, Is.EqualTo(3));
            Assert.IsTrue(group0Events.All(e => e.GroupOperator == ArithmeticOperator.Add));
            
            var keptGroup0 = group0Events.Where(e => e.Status == DieStatus.Kept).ToList();
            var droppedGroup0 = group0Events.Where(e => e.Status == DieStatus.Dropped).ToList();
            Assert.That(keptGroup0.Count, Is.EqualTo(1));
            Assert.That(droppedGroup0.Count, Is.EqualTo(2));
            Assert.That(keptGroup0[0].Value, Is.EqualTo(15)); // Highest value
            
            // Check group 1 (4d4 - Subtraction)
            var group1Events = result.Events.Where(e => e.GroupId == 1).ToList();
            Assert.That(group1Events.Count, Is.EqualTo(4));
            Assert.IsTrue(group1Events.All(e => e.GroupOperator == ArithmeticOperator.Subtract));
            Assert.IsTrue(group1Events.All(e => e.Status == DieStatus.Kept));
            
            // Check group 2 (constant +5 - Addition)
            var group2Events = result.Events.Where(e => e.GroupId == 2).ToList();
            Assert.That(group2Events.Count, Is.EqualTo(1));
            Assert.That(group2Events[0].GroupOperator, Is.EqualTo(ArithmeticOperator.Add));
            Assert.That(group2Events[0].Value, Is.EqualTo(5));
        }

        [Test]
        public void ParseRollDetailed_SingleBasicRoll_NoGroupInformation()
        {
            // Arrange
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 4, 2 });

            // Act
            var result = "2d6".ParseRollDetailed(rng);

            // Assert
            Assert.That(result.Value, Is.EqualTo(6));
            Assert.That(result.Events.Count, Is.EqualTo(2));
            
            // Single rolls should not have group information (backward compatibility)
            Assert.IsTrue(result.Events.All(e => e.GroupId == null));
            Assert.IsTrue(result.Events.All(e => e.GroupOperator == null));
        }

        [Test]
        public void ParseRollDetailed_ExplosionInGroup_PreservesGroupInformation()
        {
            // Arrange - 2d6!=6+1d4: explode 6s on d6, then add 1d4
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 6, 3, 4, 2 }); // First d6 explodes

            // Act
            var result = "2d6!=6+1d4".ParseRollDetailed(rng);

            // Assert
            var group0Events = result.Events.Where(e => e.GroupId == 0).ToList();
            var group1Events = result.Events.Where(e => e.GroupId == 1).ToList();
            
            // Group 0 should have original 2 dice + 1 explosion
            Assert.That(group0Events.Count, Is.EqualTo(3));
            Assert.IsTrue(group0Events.All(e => e.GroupOperator == ArithmeticOperator.Add));
            
            // Check explosion event has correct group info
            var explosionEvent = group0Events.FirstOrDefault(e => e.Type == DieEventType.Explosion);
            Assert.IsNotNull(explosionEvent);
            Assert.That(explosionEvent.GroupId, Is.EqualTo(0));
            Assert.That(explosionEvent.GroupOperator, Is.EqualTo(ArithmeticOperator.Add));
            
            // Group 1 should have the 1d4
            Assert.That(group1Events.Count, Is.EqualTo(1));
            Assert.That(group1Events[0].GroupOperator, Is.EqualTo(ArithmeticOperator.Add));
        }

        [Test]
        public void ParseRollDetailed_RerollInGroup_PreservesGroupInformation()
        {
            // Arrange - 2d6ro<2-1d4: reroll once if less than 2, then subtract 1d4
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 1, 4, 5, 3 }); // First die rerolls

            // Act
            var result = "2d6ro<2-1d4".ParseRollDetailed(rng);

            // Assert
            var group0Events = result.Events.Where(e => e.GroupId == 0).ToList();
            var group1Events = result.Events.Where(e => e.GroupId == 1).ToList();
            
            // Group 0 should have 2 original + 1 reroll (3 total)
            Assert.That(group0Events.Count, Is.EqualTo(3));
            Assert.IsTrue(group0Events.All(e => e.GroupOperator == ArithmeticOperator.Add));
            
            // Check reroll event has correct group info
            var rerollEvent = group0Events.FirstOrDefault(e => e.Type == DieEventType.Reroll);
            Assert.IsNotNull(rerollEvent);
            Assert.That(rerollEvent.GroupId, Is.EqualTo(0));
            Assert.That(rerollEvent.GroupOperator, Is.EqualTo(ArithmeticOperator.Add));
            
            // Group 1 should have the 1d4 with subtraction
            Assert.That(group1Events.Count, Is.EqualTo(1));
            Assert.That(group1Events[0].GroupOperator, Is.EqualTo(ArithmeticOperator.Subtract));
        }

        [Test]
        public void ParseRollDetailed_MultipleSubtractions_CorrectGroupIds()
        {
            // Arrange
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 10, 3, 2 });

            // Act
            var result = "1d20-1d6-1d8".ParseRollDetailed(rng);

            // Assert
            Assert.That(result.Value, Is.EqualTo(5)); // 10 - 3 - 2 = 5
            
            // Should have 3 groups with sequential IDs
            var groups = result.Events.GroupBy(e => e.GroupId).OrderBy(g => g.Key).ToList();
            Assert.That(groups.Count, Is.EqualTo(3));
            
            // Group 0: Add (first term is always positive)
            Assert.That(groups[0].Key, Is.EqualTo(0));
            Assert.IsTrue(groups[0].All(e => e.GroupOperator == ArithmeticOperator.Add));
            
            // Group 1: Subtract  
            Assert.That(groups[1].Key, Is.EqualTo(1));
            Assert.IsTrue(groups[1].All(e => e.GroupOperator == ArithmeticOperator.Subtract));
            
            // Group 2: Subtract
            Assert.That(groups[2].Key, Is.EqualTo(2));
            Assert.IsTrue(groups[2].All(e => e.GroupOperator == ArithmeticOperator.Subtract));
        }
    }
}