namespace DotDiceF.Core

open DotDiceF.Core
open System

/// A parser is a function that takes a string as input and returns a ParserResult.
type Parser<'T> = string -> ParserResult<'T>

module ParserCombinators =

    /// Basic Combinators
    /// 
    /// Returns a parser that always succeeds with the given value, without consuming any input.
    let pReturn x : Parser<'T> =
        fun input -> Success (x, input)

    /// Runs the parser and returns the result if it succeeds, otherwise fail.
    let runP parser input =
        parser input

    /// Choice combinator: tries the first parser, if it fails, tries the second.
    let ( <|> ) p1 p2 input =
        match p1 input with
        | Success _ as success -> success
        | Failure _ -> p2 input

    /// Apply a function to the result of a parser.
    let ( <!> ) f p input =
        match p input with
        | Success (x, rest) -> Success (f x, rest)
        | Failure err -> Failure err

    /// Sequence two parsers and combine their results.
    let ( <*> ) pF pX input =
        match pF input with
        | Failure err -> Failure err
        | Success (f, rest) ->
            match pX rest with
            | Failure err -> Failure err
            | Success (x, rest') -> Success (f x, rest')

    /// Sequence two parsers and keep the result of the first.
    let ( .>> ) p1 p2 input =
        match p1 input with
        | Failure err -> Failure err
        | Success (x, rest) ->
            match p2 rest with
            | Failure err -> Failure err
            | Success (_, rest') -> Success(x, rest')

    /// Sequence two parsers and keep the result of the second.
    let ( >>. ) p1 p2 input =
        match p1 input with
        | Failure err -> Failure err
        | Success (_, rest) -> p2 rest

    /// Other Helpful Combinators

    /// Parse a single character satisfying a predicate.
    let satisfy pred : Parser<char> =
        fun input ->
            if String.IsNullOrEmpty(input) then
                Failure "Unexpected end of input"
            else
                let c = input.[0]
                if pred c then
                    Success (c, input.[1..])
                else
                    Failure (sprintf "Unexpected character: '%c'" c)

    /// Parse a specific character.
    let pChar c : Parser<char> =
        satisfy ((=) c)

    /// Parse any character in a string.
    let anyOf chars = chars |> Seq.map pChar |> Seq.reduce (<|>)

    /// Parse one or more occurrences of a parser.
    let rec many1 p =
        (fun x xs -> x :: xs) <!> p <*> many p

    /// Parse zero or more occurrences of a parser.
    and many p =
        many1 p <|> pReturn []

    /// Parse a string.
    let pString str =
        str
        |> Seq.map pChar
        |> Seq.fold (fun acc p ->
            (fun accStr c -> accStr + string c) <!> acc <*> p) (pReturn "")

    /// Optionally parse something.
    let opt (p: Parser<'a>) : Parser<'a option> =
        let some = Some <!> p
        let none = pReturn None
        some <|> none

    /// A parser for whitespace.
     // A parser for whitespace.
    let whitespace: Parser<string> =
        many (satisfy Char.IsWhiteSpace) |> (fun p ->  (fun cl -> String.Concat(cl|> List.map string)) <!> p)

    /// Skip whitespace before and after a parser.
    let token (p: Parser<string>) : Parser<string> =
        whitespace >>. (p .>> whitespace)

    /// A parser for an integer with an optional negative sign.
    let pInteger : Parser<int> =
        let sign = opt (pChar '-')
        let digits = many1 (satisfy Char.IsDigit)
        (fun s (ds: char list) ->
            let str = String.Concat ds
            let n = Int32.Parse str
            match s with
            | Some '-' -> -n
            | _ -> n)
        <!> sign <*> digits

    /// A parser for an optional integer.
    let pOptionalInteger : Parser<int option> =
        opt pInteger