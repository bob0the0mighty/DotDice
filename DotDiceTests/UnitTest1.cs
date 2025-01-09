namespace DotDiceTests;

using DotDice;
public class Tests
{
    [SetUp]
    public void Setup()
    {
//         // Example Usage
// let testRoll rollStr =
//     match runP pRoll rollStr with
//     | Success (roll, remaining) ->
//         printfn "Parsed: %A" roll
//         if not (String.IsNullOrEmpty(remaining)) then
//             printfn "Remaining input: %s" remaining
//     | Failure err ->
//         printfn "Error: %s" err

// // Test cases
// testRoll "3d6"
// testRoll "d20kh1"
// testRoll "4d6dl1"
// testRoll "2d8!>7"
// testRoll "{3d6,1d8,4}kh2"
// testRoll "1d20ro<10"
// testRoll "{2d6sd, 1d8}f1"
// testRoll "2d10!>9kh1"
// testRoll "dF"
// testRoll "3d%"
// testRoll "{1d4, 2d6, 10}sd"
// testRoll "  { 1d4 ,   2d6  , 10    }  sd   " // Test whitespace handling
// public static void Main(string[] args)
//     {
//         TestRoll("3d6");
//         TestRoll("d20kh1");
//         TestRoll("4d6dl1");
//         TestRoll("2d8!>7");
//         TestRoll("{3d6,1d8,4}kh2");
//         TestRoll("1d20ro<10");
//         TestRoll("{2d6sd, 1d8}f1");
//         TestRoll("2d10!>9kh1");
//         TestRoll("dF");
//         TestRoll("3d%");
//         TestRoll("{1d4, 2d6, 10}sd");
//         TestRoll("  { 1d4 ,   2d6  , 10    }  sd   "); // Test whitespace handling
//     }

//     static void TestRoll(string rollStr)
//     {
//         var result = DiceParserCombinators.Roll.Run(rollStr);
//         if (result.Success)
//         {
//             Console.WriteLine($"Parsed: {result.Value}");
//         }
//         else
//         {
//             Console.WriteLine($"Error: {result.Error} at {result.Position}");
//         }
//     }

    }

    [Test]
    public void Test1()
    {
        Assert.Pass();
    }
}