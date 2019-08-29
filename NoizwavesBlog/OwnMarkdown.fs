module NoizwavesBlog.OwnMarkdown

// public interface
// not HTML safe

type MarkdownElement =
    | Span of string
    | Emphasized of string
    | Bolded of string
    | InlineLink of string * string
    | Code of string

type MarkdownParagraph =
    Paragraph of MarkdownElement list

type Markdown = MarkdownParagraph list

// Tokenizing

type private RawMarkdownText = string

type private Token =
    | Text of string
    | Underscore
    | Asterisk
    | NewLine
    | OpenBracket
    | CloseBracket
    | OpenParentheses
    | CloseParentheses
    | Backtick
    | EOF

let private tokenLength (t: Token): int =
    match t with
    | Text text -> String.length text
    | Underscore -> 1
    | Asterisk -> 1
    | NewLine -> 1
    | OpenBracket -> 1
    | CloseBracket -> 1
    | OpenParentheses -> 1
    | CloseParentheses -> 1
    | Backtick -> 1
    | EOF -> 0

// Scanner builders

type private Tokens = Token list

type private ScanResult = Token option

type private Scanner = RawMarkdownText -> ScanResult

let private thenScan (next: Scanner) (previous: Scanner): Scanner =
    fun s ->
        match previous s with
        | Some token -> Some token
        | None -> next s

// Scanners

let private textScanner (s: RawMarkdownText): ScanResult =
    let stopAt = [ '\n'; '_'; '*'; '['; ']'; '('; ')'; '`' ]

    s
    |> Seq.toList
    |> List.takeWhile (fun c -> stopAt |> List.contains c |> not)
    |> List.toArray
    |> System.String
    |> Text
    |> Some

let private charScanner (c: char) (t: Token) (s: RawMarkdownText): ScanResult =
    if s.StartsWith c then Some t else None

let private newLineScanner: Scanner = charScanner '\n' NewLine

let private underscoreScanner: Scanner = charScanner '_' Underscore

let private asteriskScanner: Scanner = charScanner '*' Asterisk

let private bracketScanner: Scanner =
    charScanner '[' OpenBracket
    |> thenScan <| charScanner ']' CloseBracket

let private parenthesesScanner: Scanner =
    charScanner '(' OpenParentheses
    |> thenScan <| charScanner ')' CloseParentheses

let private backtickScanner: Scanner = charScanner '`' Backtick

let private tokenScanner: Scanner =
    newLineScanner
    |> thenScan underscoreScanner
    |> thenScan asteriskScanner
    |> thenScan bracketScanner
    |> thenScan parenthesesScanner
    |> thenScan backtickScanner
    |> thenScan textScanner

let rec private tokenize (s: RawMarkdownText): Tokens =
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

let rec private matchStar (parser: Parser<'a>) (tokens: Tokens): ParseResult<'a list> =
    match parser tokens with
    | None -> Some([], 0)
    | Some(node, consumed) ->
        let remaining = tokens |> List.skip consumed

        match matchStar parser remaining with
        | None -> Some([ node ], consumed) // SMELL: matchStar never returns None, so we shouldn't need to match on this...
        | Some(rNodes, rConsumed) -> Some(node :: rNodes, consumed + rConsumed)

let private matchPlus (parser: Parser<'a>) (tokens: Tokens): ParseResult<'a list> =
    match matchStar parser tokens with
    | None -> None
    | Some([], _) -> None
    | Some(nodes, consumed) -> Some(nodes, consumed)

let private orParse (next: Parser<'a>) (previous: Parser<'a>): Parser<'a> =
    fun tokens ->
        match previous tokens with
        | Some result -> Some result
        | None -> next tokens

let private andParse (next: Parser<'b>) (previous: Parser<'a>): Parser<'a * 'b> =
    fun tokens ->
        match previous tokens with
        | Some(prevNode, prevConsumed) ->
            let remainingTokens = tokens |> List.skip prevConsumed
            match next remainingTokens with
            | Some(nextNode, nextConsumed) -> Some((prevNode, nextNode), prevConsumed + nextConsumed)
            | None -> None
        | None -> None

let private mapParse (lift: 'b -> 'a) (parser: Parser<'b>): Parser<'a> =
    fun tokens ->
        match parser tokens with
        | Some(node, consumed) -> Some(lift node, consumed)
        | None -> None

let private keepFirstParse (parser: Parser<'a * 'b>): Parser<'a> =
    mapParse (fun (a, _) -> a) parser

let private keepSecondParse (parser: Parser<'a * 'b>): Parser<'b> =
    mapParse (fun (_, b) -> b) parser

let private map0Parse (value: 'a) (parser: Parser<unit>): Parser<'a> =
    mapParse (fun _ -> value) parser

// Grammar is:
// Body               := Paragraph* T(EOF)
// Paragraph          := Line SubsequentLine* T(NewLine)*
// SubsequentLine     := T(NewLine) Sentence+
// Line               := Sentence+
// Sentence           := EmphasizedText
//                     | BoldedText
//                     | Text
//                     | InlineLink
//                     | Code
// Code               := T(Backtick) SimpleCode+ T(Backtick)
//                     | T(Backtick) T(Backtick) ComplexCode T(Backtick) T(Backtick)
// ComplexCode        := SimpleCode+ (T(Backtick) SimpleCode+)*
// SimpleCode         := T(Text) | T(OpenParenthesis) | T(CloseParentheses) | T(OpenBracket) | T(CloseBracket) | T(Asterisk) | T(Underscore)
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

type private SimpleCodeNode =
    | SimpleCodeTextValue of TextNode
    | SimpleCodeOpenParentheses
    | SimpleCodeCloseParentheses
    | SimpleCodeOpenBracket
    | SimpleCodeCloseBracket
    | SimpleCodeUnderscore
    | SimpleCodeAsterisk
type private ComplexCodeNode =
    | ComplexCodeSimpleValue of SimpleCodeNode list
    | ComplexCodeBacktickValue
type private CodeNode =
    | SimpleCodeValue of SimpleCodeNode list
    | ComplexCodeValue of ComplexCodeNode list

type private SentenceNode =
    | Text of TextNode
    | EmphasizedText of EmphasizedTextNode
    | BoldedText of BoldedTextNode
    | InlineLink of InlineLinkNode
    | Code of CodeNode
type private LineNode = Sentence of SentenceNode list
type private SubsequentLineNode = Sentence of SentenceNode list
type private ParagraphNode = Lines of LineNode * SubsequentLineNode list
type private BodyNode = Paragraphs of ParagraphNode list

// Parsers

let private singleTokenParser (target: Token) (tokens: Tokens): ParseResult<unit> =
    match tokens with
    | t :: _ when t = target -> Some((), 1)
    | _ -> None

let private backtickParser: Parser<unit> = singleTokenParser Backtick

let private openParenthesesParser: Parser<unit> = singleTokenParser OpenParentheses

let private closeParenthesesParser: Parser<unit> = singleTokenParser CloseParentheses

let private openBracketParser: Parser<unit> = singleTokenParser OpenBracket

let private closeBracketParser: Parser<unit> = singleTokenParser CloseBracket

let private underscoreParser: Parser<unit> = singleTokenParser Underscore

let private asteriskParser: Parser<unit> = singleTokenParser Asterisk

let private newLineParser: Parser<unit> = singleTokenParser NewLine

let private eofParser: Parser<unit> = singleTokenParser EOF

let private textParser (tokens: Tokens): ParseResult<TextNode> =
    match tokens with
    | Token.Text s :: _ -> (TextValue s, 1) |> Some
    | _ -> None

let private emphasizedTextParser (tokens: Tokens): ParseResult<EmphasizedTextNode> =
    match tokens with
    | Token.Underscore :: Token.Text s :: Token.Underscore :: _ -> (EmphasizedTextValue s, 3) |> Some
    | _ -> None

let private boldedTextParser (tokens: Tokens): ParseResult<BoldedTextNode> =
    match tokens with
    | Token.Asterisk :: Token.Asterisk :: Token.Text s :: Token.Asterisk :: Token.Asterisk :: _ ->
        (BoldedTextValue s, 5) |> Some
    | Token.Underscore :: Token.Underscore :: Token.Text s :: Token.Underscore :: Token.Underscore :: _ ->
        (BoldedTextValue s, 5) |> Some
    | _ -> None

let private inlineLinkParser (tokens: Tokens): ParseResult<InlineLinkNode> =
    match tokens with
    | Token.OpenBracket :: Token.Text name :: Token.CloseBracket :: Token.OpenParentheses :: Token.Text url :: Token.CloseParentheses :: _ ->
        (InlineLinkValue(url, name), 6) |> Some
    | _ -> None

let private simpleCodeParser: Parser<SimpleCodeNode> =
    textParser |> mapParse SimpleCodeTextValue
    |> orParse (map0Parse SimpleCodeOpenParentheses openParenthesesParser)
    |> orParse (map0Parse SimpleCodeCloseParentheses closeParenthesesParser)
    |> orParse (map0Parse SimpleCodeOpenBracket openBracketParser)
    |> orParse (map0Parse SimpleCodeCloseBracket closeBracketParser)
    |> orParse (map0Parse SimpleCodeUnderscore underscoreParser)
    |> orParse (map0Parse SimpleCodeAsterisk asteriskParser)

let private simpleCodeValueParser: Parser<CodeNode> =
    backtickParser
    |> andParse (matchPlus simpleCodeParser)
    |> keepSecondParse
    |> mapParse SimpleCodeValue
    |> andParse backtickParser
    |> keepFirstParse

let private complexCodeParser: Parser<CodeNode> =
    let simplePlus = matchPlus simpleCodeParser |> mapParse ComplexCodeSimpleValue

    let chunk =
        backtickParser |> map0Parse ComplexCodeBacktickValue
        |> andParse simplePlus
        |> mapParse (fun (f, s) -> [ f; s ])

    let doubleBacktick = backtickParser |> andParse backtickParser

    let content =
        simplePlus
        |> andParse (matchStar chunk)
        |> mapParse (fun (first, chunks) -> first :: List.concat chunks)
        |> mapParse ComplexCodeValue

    doubleBacktick
    |> andParse content
    |> keepSecondParse
    |> andParse doubleBacktick
    |> keepFirstParse

let private codeParser: Parser<CodeNode> =
    simpleCodeValueParser
    |> orParse complexCodeParser

let private sentenceParser: Parser<SentenceNode> =
    mapParse EmphasizedText emphasizedTextParser
    |> orParse <| mapParse BoldedText boldedTextParser
    |> orParse <| mapParse InlineLink inlineLinkParser
    |> orParse <| mapParse Code codeParser
    |> orParse <| mapParse Text textParser

let private lineParser: Parser<LineNode> =
    matchPlus sentenceParser
    |> mapParse LineNode.Sentence

let private subsequentLineParser: Parser<SubsequentLineNode> =
    newLineParser
    |> andParse (matchPlus sentenceParser)
    |> keepSecondParse
    |> mapParse SubsequentLineNode.Sentence

let private paragraphNodeParser: Parser<ParagraphNode> =
    lineParser
    |> andParse (matchStar subsequentLineParser)
    |> mapParse ParagraphNode.Lines
    |> andParse (matchStar newLineParser)
    |> keepFirstParse

let private bodyNodeParser: Parser<BodyNode> =
    matchStar paragraphNodeParser
    |> andParse eofParser
    |> keepFirstParse
    |> mapParse Paragraphs

let private parse (tokens: Tokens): BodyNode option =
    match bodyNodeParser tokens with
    | Some(bodyNode, consumed) ->
        if consumed = List.length tokens then
            Some bodyNode
        else
            None
    | None -> None

// AST to public types

let private renderSimpleCode (node: SimpleCodeNode): string =
    match node with
    | SimpleCodeTextValue(TextValue code) -> code
    | SimpleCodeOpenParentheses -> "("
    | SimpleCodeCloseParentheses -> ")"
    | SimpleCodeOpenBracket -> "["
    | SimpleCodeCloseBracket -> "]"
    | SimpleCodeUnderscore -> "_"
    | SimpleCodeAsterisk -> "*"

let private renderComplexCode (node: ComplexCodeNode): string =
    match node with
    | ComplexCodeSimpleValue nodes ->
        nodes
        |> List.map renderSimpleCode
        |> String.concat ""
    | ComplexCodeBacktickValue -> "`"

let private renderCode (node: CodeNode): MarkdownElement =
    match node with
    | SimpleCodeValue nodes ->
        nodes
        |> List.map renderSimpleCode
        |> String.concat ""
        |> MarkdownElement.Code
    | ComplexCodeValue nodes ->
        nodes
        |> List.map renderComplexCode
        |> String.concat ""
        |> MarkdownElement.Code

let private renderSentence (sentence: SentenceNode): MarkdownElement =
    match sentence with
    | Text(TextValue value) -> Span value
    | EmphasizedText(EmphasizedTextValue value) -> Emphasized value
    | BoldedText(BoldedTextValue value) -> Bolded value
    | InlineLink(InlineLinkValue(url, name)) -> MarkdownElement.InlineLink(url, name)
    | Code code -> renderCode code

let private renderLine (line: LineNode): MarkdownElement list =
    match line with
    | LineNode.Sentence sentences ->
        sentences
        |> List.map renderSentence

let private renderSubsequentLine (line: SubsequentLineNode): MarkdownElement list =
    match line with
    | SubsequentLineNode.Sentence sentences ->
        sentences
        |> List.map renderSentence

let private renderParagraph (paragraph: ParagraphNode): MarkdownParagraph =
    match paragraph with
    | Lines(line, subsequent) ->
        let flatten = List.fold List.append []

        let spans = renderLine line @ (flatten <| List.map renderSubsequentLine subsequent)

        MarkdownParagraph.Paragraph spans

let private render (body: BodyNode): Markdown =
    match body with
    | Paragraphs paragraphs ->
        paragraphs
        |> List.map renderParagraph

// public functions

let ParseOwn(s: string): Markdown option =
    s
    |> tokenize
    |> parse
    |> Option.map render
