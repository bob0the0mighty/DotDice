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

        // === NEW COMPREHENSIVE TEST CASES ===

        [Test]
        public void ParseRollDetailed_AllDiceTypes_GroupsCorrectly()
        {
            // Arrange - Test d4, d6, d8, d10, d12, d20, d100 combinations
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 3, 5, 7, 9, 11, 19, 85 });

            // Act
            var result = "1d4+1d6-1d8+1d10-1d12+1d20-1d100".ParseRollDetailed(rng);

            // Assert - 7 groups total
            var groups = result.Events.GroupBy(e => e.GroupId).OrderBy(g => g.Key).ToList();
            Assert.That(groups.Count, Is.EqualTo(7));

            // Verify each group has correct die type and operator
            var group0 = groups[0].ToList(); // d4 (+)
            Assert.That(group0.Count, Is.EqualTo(1));
            Assert.That(group0[0].GroupOperator, Is.EqualTo(ArithmeticOperator.Add));
            Assert.IsTrue(group0[0].DieType is DieType.Basic && ((DieType.Basic)group0[0].DieType).sides == 4);
            Assert.That(group0[0].Value, Is.EqualTo(3));

            var group1 = groups[1].ToList(); // d6 (+)
            Assert.That(group1[0].GroupOperator, Is.EqualTo(ArithmeticOperator.Add));
            Assert.IsTrue(group1[0].DieType is DieType.Basic && ((DieType.Basic)group1[0].DieType).sides == 6);

            var group2 = groups[2].ToList(); // d8 (-)
            Assert.That(group2[0].GroupOperator, Is.EqualTo(ArithmeticOperator.Subtract));
            Assert.IsTrue(group2[0].DieType is DieType.Basic && ((DieType.Basic)group2[0].DieType).sides == 8);

            var group6 = groups[6].ToList(); // d100 (-)
            Assert.That(group6[0].GroupOperator, Is.EqualTo(ArithmeticOperator.Subtract));
            Assert.IsTrue(group6[0].DieType is DieType.Basic && ((DieType.Basic)group6[0].DieType).sides == 100);
            Assert.That(group6[0].Value, Is.EqualTo(85));
        }

        [Test]
        public void ParseRollDetailed_MultipleD6WithKeepModifiers_GroupsCorrectly()
        {
            // Arrange - 4d6kh3+3d6kl2-2d6dh1+1d6dl1
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 
                6, 2, 4, 5,    // Group 0: 4d6kh3 - keep 6,5,4 drop 2
                1, 3, 6,       // Group 1: 3d6kl2 - keep 1,3 drop 6  
                4, 5,          // Group 2: 2d6dh1 - keep 4 drop 5
                2              // Group 3: 1d6dl1 - would drop if had multiple dice
            });

            // Act
            var result = "4d6kh3+3d6kl2-2d6dh1+1d6dl1".ParseRollDetailed(rng);

            // Assert
            var groups = result.Events.GroupBy(e => e.GroupId).OrderBy(g => g.Key).ToList();
            Assert.That(groups.Count, Is.EqualTo(4));

            // Group 0: 4d6kh3 - should have 4 events, 3 kept, 1 dropped
            var group0Events = groups[0].ToList();
            Assert.That(group0Events.Count, Is.EqualTo(4));
            Assert.IsTrue(group0Events.All(e => e.GroupOperator == ArithmeticOperator.Add));
            var keptGroup0 = group0Events.Where(e => e.Status == DieStatus.Kept).ToList();
            var droppedGroup0 = group0Events.Where(e => e.Status == DieStatus.Dropped).ToList();
            Assert.That(keptGroup0.Count, Is.EqualTo(3));
            Assert.That(droppedGroup0.Count, Is.EqualTo(1));

            // Group 1: 3d6kl2 - should have 3 events, 2 kept, 1 dropped
            var group1Events = groups[1].ToList();
            Assert.That(group1Events.Count, Is.EqualTo(3));
            Assert.IsTrue(group1Events.All(e => e.GroupOperator == ArithmeticOperator.Add));
            var keptGroup1 = group1Events.Where(e => e.Status == DieStatus.Kept).ToList();
            var droppedGroup1 = group1Events.Where(e => e.Status == DieStatus.Dropped).ToList();
            Assert.That(keptGroup1.Count, Is.EqualTo(2));
            Assert.That(droppedGroup1.Count, Is.EqualTo(1));

            // Group 2: 2d6dh1 - should have 2 events, 1 kept, 1 dropped
            var group2Events = groups[2].ToList();
            Assert.That(group2Events.Count, Is.EqualTo(2));
            Assert.IsTrue(group2Events.All(e => e.GroupOperator == ArithmeticOperator.Subtract));
            var keptGroup2 = group2Events.Where(e => e.Status == DieStatus.Kept).ToList();
            var droppedGroup2 = group2Events.Where(e => e.Status == DieStatus.Dropped).ToList();
            Assert.That(keptGroup2.Count, Is.EqualTo(1));
            Assert.That(droppedGroup2.Count, Is.EqualTo(1));
        }

        [Test]
        public void ParseRollDetailed_ExplosiveAndCompoundDice_PreservesGroupInfo()
        {
            // Arrange - 2d6!=6+3d10^=10-1d8!=8+2d4^=4
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 
                6, 4, 3,       // Group 0: 2d6!=6 - first die explodes (6 then 4), second is 3
                10, 5, 8, 9,   // Group 1: 3d10^=10 - first compounds (10 then 5 = 15), others are 8,9
                8, 6,          // Group 2: 1d8!=8 - explodes (8 then 6)
                4, 2, 3        // Group 3: 2d4^=4 - first compounds (4 then 2 = 6), second is 3
            });

            // Act
            var result = "2d6!=6+3d10^=10-1d8!=8+2d4^=4".ParseRollDetailed(rng);

            // Assert
            var groups = result.Events.GroupBy(e => e.GroupId).OrderBy(g => g.Key).ToList();
            Assert.That(groups.Count, Is.EqualTo(4));

            // Group 0: 2d6!=6 - should have 3 events (2 initial + 1 explosion)
            var group0Events = groups[0].ToList();
            Assert.That(group0Events.Count, Is.EqualTo(3));
            Assert.IsTrue(group0Events.All(e => e.GroupOperator == ArithmeticOperator.Add));
            var explosionEvent = group0Events.FirstOrDefault(e => e.Type == DieEventType.Explosion);
            Assert.IsNotNull(explosionEvent);
            Assert.That(explosionEvent.GroupId, Is.EqualTo(0));

            // Group 1: 3d10^=10 - should have 4 events (3 initial + 1 compound)
            var group1Events = groups[1].ToList();
            Assert.That(group1Events.Count, Is.EqualTo(4));
            Assert.IsTrue(group1Events.All(e => e.GroupOperator == ArithmeticOperator.Add));
            var compoundEvent = group1Events.FirstOrDefault(e => e.Type == DieEventType.Compound);
            Assert.IsNotNull(compoundEvent);
            Assert.That(compoundEvent.GroupId, Is.EqualTo(1));

            // Group 2: 1d8!=8 - should have 2 events (1 initial + 1 explosion)
            var group2Events = groups[2].ToList();
            Assert.That(group2Events.Count, Is.EqualTo(2));
            Assert.IsTrue(group2Events.All(e => e.GroupOperator == ArithmeticOperator.Subtract));
        }

        [Test]
        public void ParseRollDetailed_RerollModifiers_PreservesGroupInfo()
        {
            // Arrange - 3d20ro<5+2d12rc=1-1d6ro>4+4d8rc<3
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 
                2, 15, 8, 12,  // Group 0: 3d20ro<5 - first rerolls (2 then 12), others 15,8
                1, 1, 7, 5, 11, // Group 1: 2d12rc=1 - both reroll once (1->7, 1->5), then 11
                5, 2,          // Group 2: 1d6ro>4 - rerolls (5 then 2)
                2, 1, 4, 6, 8, 9, 10, 11 // Group 3: 4d8rc<3 - first two reroll (2->8, 1->9), others 4->10, 6->11
            });

            // Act
            var result = "3d20ro<5+2d12rc=1-1d6ro>4+4d8rc<3".ParseRollDetailed(rng);

            // Assert
            var groups = result.Events.GroupBy(e => e.GroupId).OrderBy(g => g.Key).ToList();
            Assert.That(groups.Count, Is.EqualTo(4));

            // Group 0: 3d20ro<5 - should have 4 events (3 initial, 1 reroll)
            var group0Events = groups[0].ToList();
            Assert.That(group0Events.Count, Is.EqualTo(4));
            Assert.IsTrue(group0Events.All(e => e.GroupOperator == ArithmeticOperator.Add));
            var rerollEvent = group0Events.FirstOrDefault(e => e.Type == DieEventType.Reroll);
            Assert.IsNotNull(rerollEvent);
            Assert.That(rerollEvent.GroupId, Is.EqualTo(0));

            // Group 2: 1d6ro>4 - should have 2 events (1 initial, 1 reroll)
            var group2Events = groups[2].ToList();
            Assert.That(group2Events.Count, Is.EqualTo(2));
            Assert.IsTrue(group2Events.All(e => e.GroupOperator == ArithmeticOperator.Subtract));
            var rerollEvent2 = group2Events.FirstOrDefault(e => e.Type == DieEventType.Reroll);
            Assert.IsNotNull(rerollEvent2);
            Assert.That(rerollEvent2.GroupId, Is.EqualTo(2));
        }

        [Test]
        public void ParseRollDetailed_FourGroupComplexExpression_FullCoverage()
        {
            // Arrange - Maximum complexity: 4d20kh2!=20+3d12dl1^=12-2d10ro<3+1d100rc>90
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 
                20, 15, 8, 3, 18,  // Group 0: 4d20kh2!=20 - keep 20,15 drop 8,3, explode 20->18
                5, 8, 12, 4,       // Group 1: 3d12dl1^=12 - drop 5, keep 8,12, compound 12->4
                2, 6, 5,           // Group 2: 2d10ro<3 - reroll 2->6, keep 5
                95, 85             // Group 3: 1d100rc>90 - reroll 95->85
            });

            // Act
            var result = "4d20kh2!=20+3d12dl1^=12-2d10ro<3+1d100rc>90".ParseRollDetailed(rng);

            // Assert
            var groups = result.Events.GroupBy(e => e.GroupId).OrderBy(g => g.Key).ToList();
            Assert.That(groups.Count, Is.EqualTo(4));

            // Group 0: 4d20kh2!=20 - 5 events (4 initial + 1 explosion), 2 kept, 3 dropped
            var group0Events = groups[0].ToList();
            Assert.That(group0Events.Count, Is.EqualTo(5));
            Assert.IsTrue(group0Events.All(e => e.GroupOperator == ArithmeticOperator.Add));
            Assert.IsTrue(group0Events.All(e => e.DieType is DieType.Basic && ((DieType.Basic)e.DieType).sides == 20));
            
            var kept0 = group0Events.Where(e => e.Status == DieStatus.Kept).ToList();
            var dropped0 = group0Events.Where(e => e.Status == DieStatus.Dropped).ToList();
            var explosion0 = group0Events.Where(e => e.Type == DieEventType.Explosion).ToList();
            
            Assert.That(kept0.Count, Is.EqualTo(2)); // Keep highest 2
            Assert.That(dropped0.Count, Is.EqualTo(3)); // Drop remaining 3 (including some from explosion)
            Assert.That(explosion0.Count, Is.EqualTo(1)); // One explosion

            // Group 1: 3d12dl1^=12 - 4 events (3 initial + 1 compound), 2 kept, 1 dropped
            var group1Events = groups[1].ToList();
            Assert.That(group1Events.Count, Is.EqualTo(4));
            Assert.IsTrue(group1Events.All(e => e.GroupOperator == ArithmeticOperator.Add));
            Assert.IsTrue(group1Events.All(e => e.DieType is DieType.Basic && ((DieType.Basic)e.DieType).sides == 12));

            // Group 2: 2d10ro<3 - 3 events (2 initial + 1 reroll), all kept
            var group2Events = groups[2].ToList();
            Assert.That(group2Events.Count, Is.EqualTo(3));
            Assert.IsTrue(group2Events.All(e => e.GroupOperator == ArithmeticOperator.Subtract));
            Assert.IsTrue(group2Events.All(e => e.DieType is DieType.Basic && ((DieType.Basic)e.DieType).sides == 10));

            // Group 3: 1d100rc>90 - 2 events (1 initial + 1 reroll), 1 kept, 1 discarded
            var group3Events = groups[3].ToList();
            Assert.That(group3Events.Count, Is.EqualTo(2));
            Assert.IsTrue(group3Events.All(e => e.GroupOperator == ArithmeticOperator.Add));
            Assert.IsTrue(group3Events.All(e => e.DieType is DieType.Basic && ((DieType.Basic)e.DieType).sides == 100));
        }

        [Test]
        public void ParseRollDetailed_SpecialDiceTypes_GroupsCorrectly()
        {
            // Arrange - Test special dice types: fudge and percentile
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 
                -1, 0, 1, 1,    // 4dF (fudge dice)
                75              // 1d100 (percentile)
            });

            // Act - Note: using "dF" for fudge dice and "d%" for percentile
            var result = "4dF+1d%".ParseRollDetailed(rng);

            // Assert
            var groups = result.Events.GroupBy(e => e.GroupId).OrderBy(g => g.Key).ToList();
            Assert.That(groups.Count, Is.EqualTo(2));

            // Group 0: 4dF (fudge dice)
            var group0Events = groups[0].ToList();
            Assert.That(group0Events.Count, Is.EqualTo(4));
            Assert.IsTrue(group0Events.All(e => e.GroupOperator == ArithmeticOperator.Add));
            Assert.IsTrue(group0Events.All(e => e.DieType is DieType.Fudge));

            // Group 1: 1d% (percentile)
            var group1Events = groups[1].ToList();
            Assert.That(group1Events.Count, Is.EqualTo(1));
            Assert.That(group1Events[0].GroupOperator, Is.EqualTo(ArithmeticOperator.Add));
            Assert.IsTrue(group1Events[0].DieType is DieType.Percent);
            Assert.That(group1Events[0].Value, Is.EqualTo(75));
        }

        [Test]
        public void ParseRollDetailed_CustomDiceValues_GroupsCorrectly()
        {
            // Arrange - Test various dN values: d3, d7, d9, d11, d13, d30
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 2, 6, 8, 10, 12, 25 });

            // Act
            var result = "1d3+1d7-1d9+1d11-1d13+1d30".ParseRollDetailed(rng);

            // Assert
            var groups = result.Events.GroupBy(e => e.GroupId).OrderBy(g => g.Key).ToList();
            Assert.That(groups.Count, Is.EqualTo(6));

            // Verify each group has correct die sides
            var expectedSides = new[] { 3, 7, 9, 11, 13, 30 };
            var expectedOperators = new[] { 
                ArithmeticOperator.Add, ArithmeticOperator.Add, ArithmeticOperator.Subtract,
                ArithmeticOperator.Add, ArithmeticOperator.Subtract, ArithmeticOperator.Add 
            };

            for (int i = 0; i < groups.Count; i++)
            {
                var groupEvents = groups[i].ToList();
                Assert.That(groupEvents.Count, Is.EqualTo(1));
                Assert.That(groupEvents[0].GroupOperator, Is.EqualTo(expectedOperators[i]));
                Assert.IsTrue(groupEvents[0].DieType is DieType.Basic);
                Assert.That(((DieType.Basic)groupEvents[0].DieType).sides, Is.EqualTo(expectedSides[i]));
            }
        }

        [Test]
        public void ParseRollDetailed_MixedModifiersWithConstants_GroupsCorrectly()
        {
            // Arrange - Mix of everything: dice, modifiers, constants
            var rng = new TestHelpers.MockRandomNumberGenerator(new List<int> { 
                4, 6, 5,        // Group 0: 3d6kh2
                8, 12,          // Group 1: 2d12
                3               // Group 2: 1d4
            });

            // Act - Expression: 3d6kh2+5-2d12+3+1d4-2
            var result = "3d6kh2+5-2d12+3+1d4-2".ParseRollDetailed(rng);

            // Assert
            var groups = result.Events.GroupBy(e => e.GroupId).OrderBy(g => g.Key).ToList();
            Assert.That(groups.Count, Is.EqualTo(6));

            // Group 0: 3d6kh2 - 3 events, 2 kept, 1 dropped
            var group0Events = groups[0].ToList();
            Assert.That(group0Events.Count, Is.EqualTo(3));
            Assert.IsTrue(group0Events.All(e => e.GroupOperator == ArithmeticOperator.Add));
            var keptCount = group0Events.Count(e => e.Status == DieStatus.Kept);
            var droppedCount = group0Events.Count(e => e.Status == DieStatus.Dropped);
            Assert.That(keptCount, Is.EqualTo(2));
            Assert.That(droppedCount, Is.EqualTo(1));

            // Group 1: constant +5
            var group1Events = groups[1].ToList();
            Assert.That(group1Events.Count, Is.EqualTo(1));
            Assert.That(group1Events[0].Value, Is.EqualTo(5));
            Assert.That(group1Events[0].GroupOperator, Is.EqualTo(ArithmeticOperator.Add));

            // Group 2: 2d12 (subtract)
            var group2Events = groups[2].ToList();
            Assert.That(group2Events.Count, Is.EqualTo(2));
            Assert.IsTrue(group2Events.All(e => e.GroupOperator == ArithmeticOperator.Subtract));

            // Group 5: constant -2
            var group5Events = groups[5].ToList();
            Assert.That(group5Events.Count, Is.EqualTo(1));
            Assert.That(group5Events[0].Value, Is.EqualTo(-2)); // Negative because it's subtraction
            Assert.That(group5Events[0].GroupOperator, Is.EqualTo(ArithmeticOperator.Subtract));
        }
    }
}