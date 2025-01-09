namespace DotDiceF.Parsers

    type ComparisonOperator =
        | Equal
        | GreaterThan
        | LessThan

    type SortDirection =
        | Ascending
        | Descending

    type Modifier =
        | Keep of count:int * keepHighest:bool // true for kh, false for k
        | Drop of count:int * dropLowest:bool  // true for dl, false for d
        | Reroll of op:ComparisonOperator * value:int * onlyOnce:bool
        | Explode of op:ComparisonOperator * value:int
        | Success of op:ComparisonOperator * value:int
        | Failure of op:ComparisonOperator * value:int
        | Sort of direction:SortDirection

    type GroupModifier =
        | GroupSuccess of op:ComparisonOperator * value:int
        | GroupFailure of op:ComparisonOperator * value:int
        | GroupSort of direction:SortDirection

    type Roll =
        | BasicRoll of numberOfDice:int * dieType:int * isFudge:bool * isPercentile: bool * modifiers:Modifier list
        | GroupedRoll of rolls:Roll list * groupModifiers:GroupModifier list
        | Constant of value:int

    module ParserModule = 
        // Parser for comparison operators
        let pComparisonOperator : Parser<ComparisonOperator> =
            (pChar '=' >>. pReturn Equal)
            <|> (pChar '>' >>. pReturn GreaterThan)
            <|> (pChar '<' >>. pReturn LessThan)

        // Parser for sort directions
        let pSortDirection : Parser<SortDirection> =
            (pChar 'a' >>. pReturn Ascending)
            <|> (pChar 'd' >>. pReturn Descending)

        // Parser for sort modifier
        let pSortModifier : Parser<Modifier> =
            pChar 's' >>. pSortDirection |> (fun p -> Sort <!> p)

        // Parser for success modifier
        let pSuccessModifier : Parser<Modifier> =
            (fun op value -> Success (op, value)) <!> pComparisonOperator <*> pInteger

        // Parser for failure modifier
        let pFailureModifier : Parser<Modifier> =
            (fun _ op value -> Failure (op, value)) <!> (pChar 'f') <*> pComparisonOperator <*> pInteger

        // Parser for explode modifier
        let pExplodeModifier : Parser<Modifier> =
            (fun _ op value -> Explode (op, value)) <!> (pChar '!') <*> pComparisonOperator <*> pInteger

        // Parser for reroll modifier
        let pRerollModifier : Parser<Modifier> =
            (fun _ onlyOnce op value -> Reroll (op, value, onlyOnce))
                <!> (pChar 'r')
                <*> (opt (pChar 'o') |> (fun p -> p |> Option.map (fun _ -> true) |> Option.defaultValue false))
                <*> pComparisonOperator
                <*> pInteger

        // Parser for keep modifier
        let pKeepModifier : Parser<Modifier> =
            (fun _ keepHighest count ->
                Keep (count |> Option.defaultValue 1, keepHighest))
                <!> (pChar 'k')
                <*> ((pChar 'h' >>. pReturn true) <|> (pChar 'l' >>. pReturn false) <|> pReturn true)
                <*> pOptionalInteger

        // Parser for drop modifier
        let pDropModifier : Parser<Modifier> =
            (fun _ dropLowest count ->
                Drop (count |> Option.defaultValue 1, dropLowest))
                <!> (pChar 'd')
                <*> ((pChar 'l' >>. pReturn true) <|> (pChar 'h' >>. pReturn false) <|> pReturn true)
                <*> pOptionalInteger

        // Parser for a single modifier
        let pModifier : Parser<Modifier> =
            pKeepModifier
            <|> pDropModifier
            <|> pRerollModifier
            <|> pExplodeModifier
            <|> pSuccessModifier
            <|> pFailureModifier
            <|> pSortModifier

        // Parser for multiple modifiers
        let pModifiers : Parser<Modifier list> =
            many pModifier

        // Parser for group modifiers - limited to success, failure, and sort for simplicity
        let pGroupModifier : Parser<GroupModifier> =
            (fun op value -> GroupSuccess (op, value)) <!> pComparisonOperator <*> pInteger
            <|> (fun _ op value -> GroupFailure (op, value)) <!> (pChar 'f') <*> pComparisonOperator <*> pInteger
            <|> (fun _ direction -> GroupSort direction) <!> (pChar 's') <*> pSortDirection
            
        let pGroupModifiers : Parser<GroupModifier list> =
            many pGroupModifier

        // Parser for a constant
        let pConstant : Parser<Roll> =
            (fun value -> Constant value) <!> pInteger

        // Parser for a basic roll
        let pBasicRoll : Parser<Roll> =
            (fun numDice dieType isFudge isPercentile modifiers ->
                BasicRoll (numDice |> Option.defaultValue 1, dieType, isFudge, isPercentile, modifiers))
            <!> pOptionalInteger
            <*> (pChar 'd' >>. (
                    (pChar 'F' >>. pReturn (0, true, false))
                    <|> (pChar '%' >>. pReturn (0, false, true))
                    <|> ((fun dt -> (dt, false, false)) <!> pInteger)
                ))
            <*> pModifiers

        // Parser for a roll group (either a basic roll or a constant)
        let pRollGroup : Parser<Roll> =
            pBasicRoll <|> pConstant

        // Parser for a grouped roll
        let pGroupedRoll : Parser<Roll> =
            (fun rolls groupModifiers -> GroupedRoll (rolls, groupModifiers))
            <!> (pChar '{' >>. many1 (pRollGroup .>>. opt (pChar ',')) .>>. pChar '}')
            <*> pGroupModifiers

        // Top-level parser for any roll
        let pRoll : Parser<Roll> =
            pGroupedRoll <|> pBasicRoll
