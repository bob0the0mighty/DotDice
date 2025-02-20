namespace DotDiceF.Ast

open DotDiceF.Ast

/// Represents a node in the abstract syntax tree (AST) for a dice roll expression.
type Roll =
    | BasicRoll of numberOfDice:int * dieType:int * isFudge:bool * isPercentile: bool * modifiers:Modifier list
    | GroupedRoll of rolls:Roll list * modifiers:Modifier list
    | Constant of value:int