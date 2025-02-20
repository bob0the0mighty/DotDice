namespace DotDiceF.Ast

open DotDiceF.Ast

/// Represents a modifier in a dice roll expression.
type Modifier =
    | Keep of count:int * keepHighest:bool // true for kh, false for kl
    | Drop of count:int * dropLowest:bool  // true for dl, false for d
    | Reroll of op:ComparisonOperator * value:int * onlyOnce:bool
    | Explode of op:ComparisonOperator * value:int
    | Compounding of op:ComparisonOperator * value:int
    | Success of op:ComparisonOperator * value:int
    | Failure of op:ComparisonOperator * value:int
    | Sort of direction:SortDirection