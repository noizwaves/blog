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
    | EOF

type private Tokens = Token list

type private Scanner = RawMarkdownText -> Token

let private textScanner (s : RawMarkdownText) : Token =
    Text s

let private tokenize (s : RawMarkdownText) : Tokens =
    [ textScanner s; EOF ]

// Parsing
// Markdown grammar is:
// Body               := Paragraph
// Paragraph          := SentenceAndEOF
// SentencesAndEOF    := Sentence T(EOF)
// Sentence           := Text
// Text               := T(Text)

type private TextNode = TextValue of string
type private SentenceNode = Text of TextNode
type private SentenceAndEOFNode = Sentence of SentenceNode
type private ParagraphNode = SentenceAndEOF of SentenceAndEOFNode
type private BodyNode = Paragraph of ParagraphNode

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
        | EOF :: _ -> Some (Sentence sentenceNode, consumed + 1)
        | _ -> None
    | None -> None

let private paragraphNodeParser (tokens : Tokens) : (ParagraphNode * int) option =
    match sentenceAndEOFParser tokens with
    | Some (sentenceAndEOFNode, consumed) -> Some (SentenceAndEOF sentenceAndEOFNode, consumed)
    | None -> None

let private bodyNodeParser (tokens : Tokens) : (BodyNode * int) option =
    match paragraphNodeParser tokens with
    | Some (paragraphNode, consumed) -> Some (Paragraph paragraphNode, consumed)
    | None -> None

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
    | SentenceAndEOF (Sentence (Text (TextValue value))) -> MarkdownParagraph.Paragraph [ Span value ]

let private render (body : BodyNode) : Markdown =
    match body with
    | Paragraph paragraph -> [ renderParagraph paragraph ]

// public functions

let ParseOwn (s : string) : Markdown option =
    s
    |> tokenize
    |> parse
    |> Option.map render