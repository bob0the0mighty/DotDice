namespace DotDiceF.Parser

open DotDiceF.Core
//open DotDiceF.Core.ParserCombinators
open DotDiceF.Ast
open FParsec

/// The main parser for Roll20 dice roll expressions.
module DiceParser =
    /// Parser for comparison operators
    let pComparisonOperator : Parser<ComparisonOperator> =
        (pChar '=' >>. pReturn Equal)
        <|> (pChar '>' >>. pReturn GreaterThan)
        <|> (pChar '<' >>. pReturn LessThan)

    /// Parser for sort directions
    let pSortDirection : Parser<SortDirection> =
        (pChar 'a' >>. pReturn Ascending)
        <|> (pChar 'd' >>. pReturn Descending)

    /// Parser for sort modifier
    let pSortModifier : Parser<Modifier> =
        (pString "sa" >>. pReturn (Sort Ascending))
        <|> (pString "sd" >>. pReturn (Sort Descending))
        |> (fun p -> Sort <!> p)

    /// Parser for success modifier
    let pSuccessModifier : Parser<Modifier> =
        (fun op value -> Success (op, value)) <!> pComparisonOperator <*> pInteger

    /// Parser for failure modifier
    let pFailureModifier : Parser<Modifier> =
        (fun _ op value -> Failure (op, value)) <!> (pChar 'f') <*> pComparisonOperator <*> pInteger

    /// Parser for explode modifier
    let pExplodeModifier : Parser<Modifier> =
        let parseExclamation = pChar '!'
        let parseOperator = parseExclamation >>. (opt pComparisonOperator)
        let parseInt = parseOperator <*> pInteger

        (fun opOpt value ->
            let op = defaultArg opOpt Equal
            Explode (op, value)) <!> parseOperator <*> parseInt

    /// Parser for compounding modifier
    let pCompoundingModifier : Parser<Modifier> =
        let isCompounding = (fun _ op value -> Compounding (op, value)) <!> (pString "!!")  
        let possibleComp = (opt pComparisonOperator) 
        let int = pInteger
        (fun p value ->
            let op = Option.fold (fun _ x -> x) Equal p
            Compounding (op, value)) <!> possibleComp <*> int

    /// Parser for reroll modifier
    let pRerollModifier : Parser<Modifier> =
        (fun _ onlyOnce op value -> Reroll (op, value, onlyOnce))
            <!> (pChar 'r')
            <*> (opt (pChar 'o') |> (fun p -> Option.map (fun _ -> true) p |> Option.defaultValue false))
            <*> pComparisonOperator
            <*> pInteger

    /// Parser for keep modifier
    let pKeepModifier : Parser<Modifier> =
        let pKeepLowest =
            (fun _ count -> Keep (Option.defaultValue 1 count, false)) <!> (pString "kl") <*> pOptionalInteger
        let pKeepHighest =
            (fun _ count -> Keep (Option.defaultValue 1 count, true)) <!> (pString "kh") <*> pOptionalInteger
        pKeepHighest <|> pKeepLowest

    /// Parser for drop modifier
    let pDropModifier : Parser<Modifier> =
        let pDropLowest =
            (fun _ count -> Drop (Option.defaultValue 1 count, true)) <!> (pString "dl") <*> pOptionalInteger
        let pDropHighest =
            (fun _ count -> Drop (Option.defaultValue 1 count, false)) <!> (pString "dh") <*> pOptionalInteger
        pDropLowest <|> pDropHighest

    /// Parser for a single modifier
    let pModifier : Parser<Modifier> =
        pKeepModifier
        <|> pDropModifier
        <|> pRerollModifier
        <|> pExplodeModifier
        <|> pCompoundingModifier
        <|> pSuccessModifier
        <|> pFailureModifier
        <|> pSortModifier

    /// Parser for multiple modifiers
    let pModifiers : Parser<Modifier list> =
        many pModifier

    /// Parser for a constant
    let pConstant : Parser<Roll> =
        (fun value -> Constant value) <!> pInteger

    /// Parser for a die type section
    let pDieTypeSection : Parser<int * bool * bool> =
            (pChar 'F' >>. pReturn (0, true, false))
        <|> (pChar '%' >>. pReturn (0, false, true))
        <|> ((fun (dt: int) -> (dt, false, false)) <!> pInteger)

    /// Parser for a basic roll
    let pBasicRoll : Parser<Roll> =
        (fun numDice (dieType, isFudge, isPercentile) modifiers ->
            BasicRoll (Option.defaultValue 1 numDice, dieType, isFudge, isPercentile, modifiers))
        <!> pOptionalInteger
        <*> (pChar 'd' >>. pDieTypeSection)
        <*> pModifiers

    /// Parser for a roll group (either a basic roll or a constant)
    let pRollGroup : Parser<Roll> =
        pBasicRoll <|> pConstant

    /// Parser for a grouped roll
    let pGroupedRoll : Parser<Roll> =
        (fun rolls modifiers -> GroupedRoll (rolls, modifiers))
        <!> (pChar '{' >>. many1 (pRollGroup .>> opt (pChar ',')) .>> pChar '}')
        <*> pModifiers

    /// Top-level parser for any roll
    let pRoll : Parser<Roll> =
        pGroupedRoll <|> pBasicRoll