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

// Parsing
// Markdown grammar is:
// Body               := Paragraph*
// Paragraph          := SentenceAndEOF
//                     | SentenceAndNewLine
// SentenceAndEOF     := Sentence T(EOF)
//                     | Sentence T(NewLine) T(EOF)
// SentenceAndNewLine := Sentence T(NewLine) T(NewLine)
// Sentence           := Text
// Text               := T(Text)

type private TextNode = TextValue of string
type private SentenceNode = Text of TextNode
type private SentenceAndEOFNode = Sentence of SentenceNode
type private SentenceAndNewLineNode = Sentence of SentenceNode
type private ParagraphNode = 
    | SentenceAndEOF of SentenceAndEOFNode
    | SentenceAndNewLine of SentenceAndNewLineNode
type private BodyNode = Paragraphs of ParagraphNode list

let private textParser (tokens : Tokens) : (TextNode * int) option =
    match tokens with
    | Token.Text s :: _ -> (TextValue s, 1) |> Some
    | _ -> None

let private sentenceParser (tokens : Tokens) : (SentenceNode * int) option =
    match textParser tokens with
    | Some (textNode, consumed) -> Some (Text textNode, consumed)
    | None -> None

let private sentenceAndEOFParser (tokens : Tokens) : (SentenceAndEOFNode * int) option =
    match sentenceParser tokens with
    | Some (sentenceNode, consumed) ->
        match List.skip consumed tokens with
        | EOF :: _ -> Some (SentenceAndEOFNode.Sentence sentenceNode, consumed + 1)
        | NewLine :: EOF ::_ -> Some (SentenceAndEOFNode.Sentence sentenceNode, consumed + 2)
        | _ -> None
    | None -> None

let private sentenceAndNewLineParser (tokens : Tokens) : (SentenceAndNewLineNode * int) option =
    match sentenceParser tokens with
    | Some (sentenceNode, consumed) ->
        match List.skip consumed tokens with
        | NewLine :: NewLine :: _ -> Some (SentenceAndNewLineNode.Sentence sentenceNode, consumed + 2)
        | _ -> None
    | None -> None

let private paragraphNodeParser (tokens : Tokens) : (ParagraphNode * int) option =
    let eofMatch = sentenceAndEOFParser tokens
    let newLineMatch = sentenceAndNewLineParser tokens
    match eofMatch, newLineMatch with
    | ( Some (sentenceAndEOFNode, consumed), _ ) -> Some (SentenceAndEOF sentenceAndEOFNode, consumed)
    | ( _, Some (sentenceAndNewLineNode, consumed) ) -> Some (SentenceAndNewLine sentenceAndNewLineNode, consumed)
    | ( None, None )  -> None

let rec private matchStarParagraphNodeParser (tokens : Tokens) : ParagraphNode list * int =
    match paragraphNodeParser tokens with
    | None -> [], 0
    | Some (paragraphNode, consumed) ->
        let more, moreConsumed =
            tokens
            |> List.skip consumed
            |> matchStarParagraphNodeParser
        
        paragraphNode :: more, consumed + moreConsumed

let private bodyNodeParser (tokens : Tokens) : (BodyNode * int) option =
    let paragraphs, consumed = matchStarParagraphNodeParser tokens

    // TODO: this is always `Some`
    // probably because of the match star in the grammar
    (Paragraphs paragraphs, consumed) |> Some

let private parse (tokens : Tokens) : BodyNode option =
    match bodyNodeParser tokens with
    | Some (bodyNode, consumed) ->
        if consumed = List.length tokens then
            Some bodyNode
        else
            None        
    | None -> None

// AST to public types

let private renderParagraph (paragraph : ParagraphNode) : MarkdownParagraph =
    match paragraph with
    | SentenceAndEOF (SentenceAndEOFNode.Sentence (Text (TextValue value))) -> MarkdownParagraph.Paragraph [ Span value ]
    | SentenceAndNewLine (SentenceAndNewLineNode.Sentence (Text (TextValue value))) -> MarkdownParagraph.Paragraph [ Span value ]

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