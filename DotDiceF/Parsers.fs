namespace DotDiceF.Parsers

open System

// Define the parser result type
type ParserResult<'T> =
    | Success of 'T * string
    | Failure of string

// Define a parser type
type Parser<'T> = string -> ParserResult<'T>


    module CombinatorModule =
        // Basic combinators

        // Return a parser that always succeeds with the given value
        let pReturn x : Parser<'T> =
            fun input -> Success (x, input)

        // Run the parser and return the result if it succeeds, otherwise fail
        let runP parser input =
            parser input

        // Choice combinator: try the first parser, if it fails, try the second
        let ( <|> ) p1 p2 input =
            match p1 input with
            | Success _ as success -> success
            | Failure _ -> p2 input

        // Apply a function to the result of a parser
        let ( <!> ) f p input =
            match p input with
            | Success (x, rest) -> Success (f x, rest)
            | Failure err -> Failure err

        // Sequence two parsers and combine their results
        let ( <*> ) pF pX input =
            match pF input with
            | Failure err -> Failure err
            | Success (f, rest) ->
                match pX rest with
                | Failure err -> Failure err
                | Success (x, rest') -> Success (f x, rest')

        // Sequence two parsers and keep the result of the first
        let ( .>>. ) p1 p2 input =
            (fun x _ -> x) <!> p1 <*> p2 input

        // Sequence two parsers and keep the result of the second
        let ( >>. ) p1 p2 input =
            (fun _ y -> y) <!> p1 <*> p2 input

        // Other helpful combinators

        // Parse a single character satisfying a predicate
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

        // Parse a specific character
        let pChar c : Parser<char> =
            satisfy ((=) c)

        // Parse any character in a string
        let anyOf chars = chars |> Seq.map pChar |> Seq.reduce (<|>)

        // Parse one or more occurrences of a parser
        let rec many1 p =
            (fun x xs -> x :: xs) <!> p <*> many p

        // Parse zero or more occurrences of a parser
        and many p =
            many1 p <|> pReturn []

        // Parse a string
        let pString str =
            str
            |> Seq.map pChar
            |> Seq.reduce (>>)
            |> (fun p -> p .>>. whitespace) // Consume trailing whitespace

        // Optionally parse something
        let opt p =
            p <|> pReturn None

        // A parser for whitespace
        let whitespace: Parser<string> =
            many1 (satisfy Char.IsWhiteSpace) |> many >>. pReturn ""

        // Skip whitespace before and after a parser
        let token p =
            whitespace >>. p .>>. whitespace

        // A parser for an integer with an optional negative sign
        let pInteger : Parser<int> =
            let negate x = -x
            let sign = opt (pChar '-')
            let digits = many1 (satisfy Char.IsDigit)
            (fun s ds ->
                let str = String.Concat ds
                let n = Int32.Parse str
                match s with
                | Some '-' -> negate n
                | _ -> n)
            <!> sign <*> digits

        // A parser for an optional integer
        let pOptionalInteger : Parser<int option> =
            opt pInteger