module NoizwavesBlog.OwnMarkdown

// public interface

type MarkdownElement
    = Span of string
    | Emphasized of string
    | Bolded of string
    | InlineLink of string * string

type MarkdownParagraph =
    Paragraph of MarkdownElement list

type Markdown = MarkdownParagraph list

// Tokenizing

type private RawMarkdownText = string

type private Token
    = Text of string
    | Underscore
    | Asterisk
    | NewLine
    | OpenBracket
    | CloseBracket
    | OpenParentheses
    | CloseParentheses
    | EOF

let private tokenLength (t: Token) : int =
    match t with
    | Text text -> String.length text
    | Underscore -> 1
    | Asterisk -> 1
    | NewLine -> 1
    | OpenBracket -> 1
    | CloseBracket -> 1
    | OpenParentheses -> 1
    | CloseParentheses -> 1
    | EOF -> 0

// Scanner builders

type private Tokens = Token list

type private ScanResult = Token option

type private Scanner = RawMarkdownText -> ScanResult

let private thenScan (next : Scanner) (previous : Scanner) : Scanner =
    fun s ->
        match previous s with
        | Some token -> Some token
        | None -> next s

// Scanners

let private textScanner (s : RawMarkdownText) : ScanResult =
    let stopAt = [ '\n'; '_'; '*'; '['; ']'; '('; ')' ]

    s
    |> Seq.toList
    |> List.takeWhile (fun c -> stopAt |> List.contains c |> not)
    |> List.toArray
    |> System.String
    |> Text
    |> Some

let private charScanner (c : char) (t : Token) (s : RawMarkdownText) : ScanResult =
    if s.StartsWith c then Some t else None

let private newLineScanner : Scanner = charScanner '\n' NewLine

let private underscoreScanner : Scanner = charScanner '_' Underscore

let private asteriskScanner : Scanner = charScanner '*' Asterisk

let private bracketScanner : Scanner =
    charScanner '[' OpenBracket
    |> thenScan <| charScanner ']' CloseBracket

let private parenthesesScanner : Scanner =
    charScanner '(' OpenParentheses
    |> thenScan <| charScanner ')' CloseParentheses

let private tokenScanner : Scanner =
    newLineScanner
    |> thenScan underscoreScanner
    |> thenScan asteriskScanner
    |> thenScan bracketScanner
    |> thenScan parenthesesScanner
    |> thenScan textScanner

let rec private tokenize (s : RawMarkdownText) : Tokens =
    if s = "" then
        [ EOF ]
    else    
        match tokenScanner s with
        | Some token ->
            let consumed = tokenLength token
            let untokenized = String.substring consumed s
            token :: (tokenize untokenized)
        | None -> failwith "no token match"

// Grammar builders

type private ParseResult<'n> = ('n * int) option

type private Parser<'a> = Tokens -> ParseResult<'a>

let rec private matchStar (parser : Parser<'a>) (tokens : Tokens) : 'a list * int =
    match parser tokens with
    | None -> [], 0
    | Some (a, consumed) ->
        let more, moreConsumed =
            tokens
            |> List.skip consumed
            |> matchStar parser

        a :: more, consumed + moreConsumed

let private matchPlus (parser : Parser<'a>) (tokens : Tokens) : ParseResult<'a list> =
    match matchStar parser tokens with
    | [], _ -> None
    | nodes, consumed -> Some (nodes, consumed)

// Grammar is:
// Body               := Paragraph* T(EOF)
// Paragraph          := Line SubsequentLine* T(NewLine)*
// SubsequentLine     := T(NewLine) Sentence+
// Line               := Sentence+
// Sentence           := EmphasizedText
//                     | BoldedText
//                     | Text
//                     | InlineLink
// InlineLink         := T(OpenBracket) T(Text) T(CloseBracket) T(OpenParentheses) T(Text) T(CloseParentheses)
// EmphasizedText     := T(Underscore) T(Text) T(Underscore)
// BoldedText         := T(Asterisk) T(Asterisk) T(Text) T(Asterisk) T(Asterisk)
// Text               := T(Text)

// Known grammar issues
// - non-terminating paragraphs must have a T(NewLine)
//   - * should be + for these

// AST

type private TextNode = TextValue of string
type private EmphasizedTextNode = EmphasizedTextValue of string
type private BoldedTextNode = BoldedTextValue of string
type private InlineLinkNode = InlineLinkValue of string * string
type private SentenceNode
    = Text of TextNode
    | EmphasizedText of EmphasizedTextNode
    | BoldedText of BoldedTextNode
    | InlineLink of InlineLinkNode
type private LineNode = Sentence of SentenceNode list
type private SubsequentLineNode = Sentence of SentenceNode list
type private ParagraphNode = Lines of LineNode * SubsequentLineNode list
type private BodyNode = Paragraphs of ParagraphNode list

// Parsers

let private textParser (tokens : Tokens) : ParseResult<TextNode> =
    match tokens with
    | Token.Text s :: _ -> (TextValue s, 1) |> Some
    | _ -> None

let private emphasizedTextParser (tokens : Tokens) : ParseResult<EmphasizedTextNode> =
    match tokens with
    | Token.Underscore :: Token.Text s :: Token.Underscore :: _ -> (EmphasizedTextValue s, 3) |> Some
    | _ -> None

let private boldedTextParser (tokens : Tokens) : ParseResult<BoldedTextNode> =
    match tokens with
    | Token.Asterisk :: Token.Asterisk :: Token.Text s :: Token.Asterisk :: Token.Asterisk :: _ ->
        (BoldedTextValue s, 5) |> Some
    | Token.Underscore :: Token.Underscore :: Token.Text s :: Token.Underscore :: Token.Underscore :: _ ->
        (BoldedTextValue s, 5) |> Some
    | _ -> None

let private inlineLinkParser (tokens : Tokens) : ParseResult<InlineLinkNode> =
    match tokens with
    | Token.OpenBracket :: Token.Text name :: Token.CloseBracket :: Token.OpenParentheses :: Token.Text url :: Token.CloseParentheses :: _ ->
        (InlineLinkValue (url, name), 6) |> Some
    | _ -> None

let private sentenceParser (tokens : Tokens) : ParseResult<SentenceNode> =
    match emphasizedTextParser tokens, boldedTextParser tokens, inlineLinkParser tokens, textParser tokens with
    | Some (emphasizedTextNode, consumed), _, _, _ -> Some (EmphasizedText emphasizedTextNode, consumed)
    | _, Some (boldedTextNode, consumed), _, _ -> Some (BoldedText boldedTextNode, consumed)
    | _, _, Some (inlineLinkNode, consumed), _ -> Some (InlineLink inlineLinkNode, consumed)
    | _, _, _, Some (textNode, consumed) -> Some (Text textNode, consumed)
    | None, None, None, None -> None

let private lineParser (tokens : Tokens) : ParseResult<LineNode> =
    match matchPlus sentenceParser tokens with
    | Some (sentenceNodes, consumed) -> Some (LineNode.Sentence sentenceNodes, consumed)
    | None -> None

let private subsequentLineParser (tokens : Tokens) : ParseResult<SubsequentLineNode> =
    match tokens with
    | NewLine :: other ->
        match matchPlus sentenceParser other with
        | Some (sentenceNodes, consumed) -> Some (SubsequentLineNode.Sentence sentenceNodes, consumed + 1)
        | None -> None
    | _ -> None

let private newLineParser (tokens : Tokens) : ParseResult<unit> =
    match tokens with
    | NewLine :: _ -> Some <| ((), 1)
    | _ -> None

let private paragraphNodeParser (tokens : Tokens) : ParseResult<ParagraphNode> =
    match lineParser tokens with
    | Some (line, consumed) ->
        let subsequentLines, subsequentConsumed = 
            tokens
            |> List.skip consumed
            |> matchStar subsequentLineParser

        let paragraph = ParagraphNode.Lines (line, subsequentLines)
        let totalConsumed = consumed + subsequentConsumed

        // trailing new lines
        let _, newLinesConsumed =
            tokens
            |> List.skip totalConsumed
            |> matchStar newLineParser

        (paragraph, totalConsumed + newLinesConsumed) |> Some
    | None -> None

let private bodyNodeParser (tokens : Tokens) : ParseResult<BodyNode> =
    let paragraphs, consumed = matchStar paragraphNodeParser tokens

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

let private renderSentence (sentence : SentenceNode) : MarkdownElement =
    match sentence with
    | Text (TextValue value) -> Span value
    | EmphasizedText (EmphasizedTextValue value) -> Emphasized value
    | BoldedText (BoldedTextValue value) -> Bolded value
    | InlineLink (InlineLinkValue (url, name)) -> MarkdownElement.InlineLink (url, name)

let private renderLine (line : LineNode) : MarkdownElement list =
    match line with
    | LineNode.Sentence sentences ->
        sentences
        |> List.map renderSentence

let private renderSubsequentLine (line : SubsequentLineNode) : MarkdownElement list =
    match line with
    | SubsequentLineNode.Sentence sentences ->
        sentences
        |> List.map renderSentence

let private renderParagraph (paragraph : ParagraphNode) : MarkdownParagraph =
    match paragraph with
    | Lines (line, subsequent) ->
        let flatten = List.fold List.append []

        let spans = renderLine line @ (flatten <| List.map renderSubsequentLine subsequent)

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