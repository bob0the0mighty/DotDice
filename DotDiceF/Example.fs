namespace DotDiceF

open DotDiceF.Core
open DotDiceF.Parser

/// Example usage of the dice parser.
module Example =
    let testRoll rollStr =
        match runP DiceParser.pRoll rollStr with
        | Success (roll, remaining) ->
            printfn "Parsed: %A" roll
            if not (String.IsNullOrEmpty(remaining)) then
                printfn "Remaining input: %s" remaining
        | Failure err ->
            printfn "Error: %s" err

    [<EntryPoint>]
    let main argv =
        testRoll "3d6"
        testRoll "d20kh1"
        testRoll "4d6dl1"
        testRoll "2d8!7"
        testRoll "2d8!<7"
        testRoll "2d8!!5"
        testRoll "2d8!!>5"
        testRoll "2d8!!<5"
        testRoll "{3d6,1d8,4}kh2"
        testRoll "1d20ro<10"
        testRoll "{2d6sa, 1d8}f1"
        testRoll "2d10!>9kh1"
        testRoll "dF"
        testRoll "3d%"
        testRoll "{1d4, 2d6, 10}sd"
        testRoll "  { 1d4 ,   2d6  , 10    }  sd   " // Test whitespace handling

        0 // Return 0 to indicate successful execution