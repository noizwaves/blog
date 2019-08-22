module NoizwavesBlog.OwnMarkdown

// public interface

type MarkdownElement =
    Span of string

type MarkdownParagraph =
    Paragraph of MarkdownElement list

type Markdown = MarkdownParagraph list

// Tokenizing

type private RawMarkdownText = string

type private Token
    = Text of string
    | NewLine
    | EOF

let private tokenLength (t: Token) : int =
    match t with
    | Text text -> String.length text
    | NewLine -> 1
    | EOF -> 0

type private Tokens = Token list

// type private Scanner = RawMarkdownText -> Token

let private textScanner (s : RawMarkdownText) : Token option =
    s
    |> Seq.toList
    |> List.takeWhile (fun c -> c <> '\n')
    |> List.toArray
    |> System.String
    |> Text
    |> Some

let private newLineScanner (s : RawMarkdownText) : Token option =
    if s.StartsWith '\n' then
        Some NewLine
    else
        None

let rec private tokenize (s : RawMarkdownText) : Tokens =
    if s = "" then
        [ EOF ]
    else    
        let newLineMatch = newLineScanner s
        let textMatch = textScanner s

        match (newLineMatch, textMatch) with
        | Some token, _ ->
            let consumed = tokenLength token
            let untokenized = String.substring consumed s
            token :: (tokenize untokenized)
        | _, Some token ->
            let consumed = tokenLength token
            let untokenized = String.substring consumed s
            token :: (tokenize untokenized)
        | None, None -> failwith "no token match"

// Grammar builders

type private Parser<'a> = Tokens -> ('a * int) option

let rec private matchStar (parser : Parser<'a>) (tokens : Tokens) : 'a list * int =
    match parser tokens with
    | None -> [], 0
    | Some (a, consumed) ->
        let more, moreConsumed =
            tokens
            |> List.skip consumed
            |> matchStar parser

        a :: more, consumed + moreConsumed

// Parsing
// Markdown grammar is:
// Body               := Paragraph* T(EOF)
// Paragraph          := Line SubsequentLine* T(NewLine)*
// SubsequentLine     := T(NewLine) Sentence
// Line               := Sentence
// Sentence           := Text
// Text               := T(Text)

type private TextNode = TextValue of string
type private SentenceNode = Text of TextNode
type private LineNode = Sentence of SentenceNode
type private SubsequentLineNode = Sentence of SentenceNode
type private ParagraphNode = Lines of LineNode * SubsequentLineNode list
type private BodyNode = Paragraphs of ParagraphNode list

let private textParser (tokens : Tokens) : (TextNode * int) option =
    match tokens with
    | Token.Text s :: _ -> (TextValue s, 1) |> Some
    | _ -> None

let private sentenceParser (tokens : Tokens) : (SentenceNode * int) option =
    match textParser tokens with
    | Some (textNode, consumed) -> Some (Text textNode, consumed)
    | None -> None

let private lineParser (tokens : Tokens) : (LineNode * int) option =
    match sentenceParser tokens with
    | Some (sentenceNode, consumed) -> Some (LineNode.Sentence sentenceNode, consumed)
    | None -> None

let private subsequentLineParser (tokens : Tokens) : (SubsequentLineNode * int) option =
    match tokens with
    | NewLine :: other ->
        match sentenceParser other with
        | Some (sentenceNode, consumed) -> Some (SubsequentLineNode.Sentence sentenceNode, consumed + 1)
        | None -> None
    | _ -> None

let private matchStarSubsequentLineNodeParser (tokens : Tokens) : SubsequentLineNode list * int =
    matchStar subsequentLineParser tokens

let private newLineParser (tokens : Tokens) : (unit * int) option =
    match tokens with
    | NewLine :: _ -> Some <| ((), 1)
    | _ -> None

let private matchStarNewLineParser (tokens : Tokens) : unit list * int =
    matchStar newLineParser tokens

let private paragraphNodeParser (tokens : Tokens) : (ParagraphNode * int) option =
    match lineParser tokens with
    | Some (line, consumed) ->
        let subsequentLines, subsequentConsumed = 
            tokens
            |> List.skip consumed
            |> matchStarSubsequentLineNodeParser

        let paragraph = ParagraphNode.Lines (line, subsequentLines)
        let totalConsumed = consumed + subsequentConsumed

        // trailing new lines
        let _, newLinesConsumed =
            tokens
            |> List.skip totalConsumed
            |> matchStarNewLineParser

        (paragraph, totalConsumed + newLinesConsumed) |> Some
    | None -> None

let private matchStarParagraphNodeParser (tokens : Tokens) : ParagraphNode list * int =
    matchStar paragraphNodeParser tokens

let private bodyNodeParser (tokens : Tokens) : (BodyNode * int) option =
    let paragraphs, consumed = matchStarParagraphNodeParser tokens

    let remaining =
        tokens
        |> List.skip consumed

    match remaining with
    | [ EOF ] -> (Paragraphs paragraphs, consumed + 1) |> Some
    | _ -> None

let private parse (tokens : Tokens) : BodyNode option =
    match bodyNodeParser tokens with
    | Some (bodyNode, consumed) ->
        if consumed = List.length tokens then
            Some bodyNode
        else
            None        
    | None -> None

// AST to public types

let private renderLine (line : LineNode) : MarkdownElement =
    match line with
    | LineNode.Sentence (Text (TextValue value)) -> Span value

let private renderSubsequentLine (line : SubsequentLineNode) =
    match line with
    | SubsequentLineNode.Sentence (Text (TextValue value)) -> Span value

let private renderParagraph (paragraph : ParagraphNode) : MarkdownParagraph =
    match paragraph with
    | Lines (line, subsequent) ->
        let spans = renderLine line :: (List.map renderSubsequentLine subsequent)

        MarkdownParagraph.Paragraph spans

let private render (body : BodyNode) : Markdown =
    match body with
    | Paragraphs paragraphs ->
        paragraphs
        |> List.map renderParagraph

// public functions

let ParseOwn (s : string) : Markdown option =
    s
    |> tokenize
    |> parse
    |> Option.map render