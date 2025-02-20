using System;
using NUnit.Framework;
using DotDice;

namespace DotDice.Tests
{
    [TestFixture]
    public class ParserCombinatorsTest
    {
        [Test]
        public void Run_ShouldReturnSuccessResult_WhenParserSucceeds()
        {
            // Arrange
            var parser = ParserCombinators.Return(42);

            // Act
            var result = parser.Run("input");

            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual(42, result.Value);
            Assert.AreEqual(0, result.Position);
        }

        [Test]
        public void Run_ShouldReturnFailureResult_WhenParserFails()
        {
            // Arrange
            var parser = new Parser<int>((input, position) => ParserResult<int>.Fail("Error", position));

            // Act
            var result = parser.Run("input");

            // Assert
            Assert.IsFalse(result.Success);
            Assert.AreEqual("Error", result.Error);
            Assert.AreEqual(0, result.Position);
        }
    }
}