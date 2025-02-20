namespace DotDiceF.Core

/// Represents the result of a parsing operation.
type ParserResult<'T> =
    | Success of 'T * string  // Success: Contains the parsed value and the remaining input string.
    | Failure of string      // Failure: Contains an error message.