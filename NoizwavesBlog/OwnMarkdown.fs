module NoizwavesBlog.OwnMarkdown

// public interface
// not HTML safe

type MarkdownElement =
    | Span of string
    | Emphasized of string
    | Bolded of string
    | InlineLink of string * string
    | Code of string

type MarkdownListItem =
    | ListItem of MarkdownElement list

type MarkdownParagraph =
    | Paragraph of MarkdownElement list
    | Heading1 of MarkdownElement list
    | Heading2 of MarkdownElement list
    | Heading3 of MarkdownElement list
    | OrderedList of MarkdownListItem list
    | CodeBlock of string
    | QuoteBlock of string

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
    | Hash
    | HashSpace
    | GreaterThanSpace
    | OneDotSpaceSpace
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
    | Hash -> 1
    | HashSpace -> 2
    | GreaterThanSpace -> 2
    | OneDotSpaceSpace -> 4
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
    // BUG: we should be stopping at "# "?
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

let private hashSpaceScanner (text: RawMarkdownText): ScanResult =
    if text.StartsWith "# " then Some HashSpace else None

let private greaterThanSpaceScanner (text: RawMarkdownText): ScanResult =
    if text.StartsWith "> " then Some GreaterThanSpace else None

let private oneDotSpaceSpaceScanner (text: RawMarkdownText): ScanResult =
    if text.StartsWith "1.  " then Some OneDotSpaceSpace else None

let private hashScanner: Scanner = charScanner '#' Hash

let private tokenScanner: Scanner =
    newLineScanner
    |> thenScan underscoreScanner
    |> thenScan asteriskScanner
    |> thenScan bracketScanner
    |> thenScan parenthesesScanner
    |> thenScan backtickScanner
    |> thenScan hashSpaceScanner
    |> thenScan hashScanner
    |> thenScan greaterThanSpaceScanner
    |> thenScan oneDotSpaceSpaceScanner
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
//                     | T(Hash) T(Hash) T(HashSpace) Sentence* T(NewLine)*
//                     | T(Hash) T(HashSpace) Sentence* T(NewLine)*
//                     | T(HashSpace) Sentence* T(NewLine)*
//                     | CodeBlock T(NewLine)*
//                     | QuoteBlockLine (T(NewLine) QuoteBlockLine)* T(NewLine)*
//                     | ListItemLine (T(NewLine) ListItemLine)* T(NewLine)*
// ListItemLine       := T(OneDotSpaceSpace) Sentence+
// QuoteBlockLine     := T(GreaterThanSpace) QuoteBlockPart+
// QuoteBlockPart     := T(Text) | T(OpenParenthesis) | T(CloseParentheses) | T(OpenBracket) | T(CloseBracket) | T(Asterisk) | T(Underscore) | T(Hash) | T(HashSpace) | T(Backtick) | T(GreaterThanSpace)
// SubsequentLine     := T(NewLine) SentenceStart Sentence*
// Line               := SentenceStart Sentence*
// SentenceStart      := EmphasizedText
//                     | BoldedText
//                     | Text
//                     | InlineLink
//                     | Code
// Sentence           := EmphasizedText
//                     | BoldedText
//                     | Text
//                     | InlineLink
//                     | Code
//                     | T(HashSpace)
// CodeBlock          := TripleBacktick T(NewLine) CodeBlockLine+ TripleBacktick
// CodeBlockLine      := CodeBlockPart+ (T)NewLine
// CodeBlockPart      := T(Text) | T(OpenParenthesis) | T(CloseParentheses) | T(OpenBracket) | T(CloseBracket) | T(Asterisk) | T(Underscore) | T(Hash) | T(HashSpace) | T(GreaterThanSpace)
// TripleBacktick     := T(Backtick) T(Backtick) T(Backtick)
// Code               := T(Backtick) SimpleCode+ T(Backtick)
//                     | T(Backtick) T(Backtick) ComplexCode T(Backtick) T(Backtick)
// ComplexCode        := SimpleCode+ (T(Backtick) SimpleCode+)*
// SimpleCode         := T(Text) | T(OpenParenthesis) | T(CloseParentheses) | T(OpenBracket) | T(CloseBracket) | T(Asterisk) | T(Underscore) | T(Hash) | T(HashSpace) | T(GreaterThanSpace)
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
    | SimpleCodeHash
    | SimpleCodeHashSpace
    | SimpleCodeGreaterThanSpace
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
    | HashSpace // BUG: do we need this?
type private LineNode = Sentence of SentenceNode list
type private SubsequentLineNode = Sentence of SentenceNode list

type private CodeBlockPartNode =
    | CodeBlockPartTextValue of TextNode
    | CodeBlockPartOpenParentheses
    | CodeBlockPartCloseParentheses
    | CodeBlockPartOpenBracket
    | CodeBlockPartCloseBracket
    | CodeBlockPartUnderscore
    | CodeBlockPartAsterisk
    | CodeBlockPartHash
    | CodeBlockPartHashSpace
    | CodeBlockPartGreaterThanSpace
type private CodeBlockLineNode =
    | CodeBlockLineValue of CodeBlockPartNode list

type private QuoteBlockPartNode =
    | QuoteBlockPartText of TextNode
    | QuoteBlockPartOpenParentheses
    | QuoteBlockPartCloseParentheses
    | QuoteBlockPartOpenBracket
    | QuoteBlockPartCloseBracket
    | QuoteBlockPartUnderscore
    | QuoteBlockPartAsterisk
    | QuoteBlockPartHash
    | QuoteBlockPartHashSpace
    | QuoteBlockPartBacktick
    | QuoteBlockPartGreaterThanSpace
type private QuoteBlockLineNode =
    | QuoteBlockLineValue of QuoteBlockPartNode list

type private OrderedListLineNode =
    | OrderedListLineValue of SentenceNode list

type private ParagraphNode =
    | Lines of LineNode * SubsequentLineNode list
    | CodeBlock of CodeBlockLineNode list
    | QuoteBlock of QuoteBlockLineNode list
    | Heading1 of SentenceNode list
    | Heading2 of SentenceNode list
    | Heading3 of SentenceNode list
    | OrderedList of OrderedListLineNode list
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

let private hashParser: Parser<unit> = singleTokenParser Token.Hash

let private hashSpaceParser: Parser<unit> = singleTokenParser Token.HashSpace

let private greaterThanSpaceParser: Parser<unit> = singleTokenParser Token.GreaterThanSpace

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
    |> orParse (map0Parse SimpleCodeHash hashParser)
    |> orParse (map0Parse SimpleCodeHashSpace hashSpaceParser)
    |> orParse (map0Parse SimpleCodeGreaterThanSpace greaterThanSpaceParser)

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
    |> orParse <| map0Parse HashSpace hashSpaceParser
    |> orParse <| mapParse Text textParser

let private sentenceStartParser: Parser<SentenceNode> =
    mapParse EmphasizedText emphasizedTextParser
    |> orParse <| mapParse BoldedText boldedTextParser
    |> orParse <| mapParse InlineLink inlineLinkParser
    |> orParse <| mapParse Code codeParser
    |> orParse <| mapParse Text textParser

let private lineParser: Parser<LineNode> =
    sentenceStartParser
    |> andParse (matchStar sentenceParser)
    |> mapParse (fun (p, pps) -> p :: pps)
    |> mapParse LineNode.Sentence

let private subsequentLineParser: Parser<SubsequentLineNode> =
    newLineParser
    |> andParse sentenceStartParser
    |> keepSecondParse
    |> andParse (matchStar sentenceParser)
    |> mapParse (fun (p, pps) -> p :: pps)
    |> mapParse SubsequentLineNode.Sentence

let private paragraphLinesParser: Parser<ParagraphNode> =
    lineParser
    |> andParse (matchStar subsequentLineParser)
    |> mapParse ParagraphNode.Lines
    |> andParse (matchStar newLineParser)
    |> keepFirstParse

let private paragraphHeading1Parser: Parser<ParagraphNode> =
    hashSpaceParser
    |> andParse (matchStar sentenceParser)
    |> keepSecondParse
    |> andParse (matchStar newLineParser)
    |> keepFirstParse
    |> mapParse ParagraphNode.Heading1

let private paragraphHeading2Parser: Parser<ParagraphNode> =
    hashParser
    |> andParse hashSpaceParser
    |> andParse (matchStar sentenceParser)
    |> keepSecondParse
    |> andParse (matchStar newLineParser)
    |> keepFirstParse
    |> mapParse ParagraphNode.Heading2

let private paragraphHeading3Parser: Parser<ParagraphNode> =
    hashParser
    |> andParse hashParser
    |> andParse hashSpaceParser
    |> andParse (matchStar sentenceParser)
    |> keepSecondParse
    |> andParse (matchStar newLineParser)
    |> keepFirstParse
    |> mapParse ParagraphNode.Heading3

let private codeBlockPartParser: Parser<CodeBlockPartNode> =
    textParser
    |> mapParse CodeBlockPartTextValue
    |> orParse (openParenthesesParser |> map0Parse CodeBlockPartOpenParentheses)
    |> orParse (closeParenthesesParser |> map0Parse CodeBlockPartCloseParentheses)
    |> orParse (openBracketParser |> map0Parse CodeBlockPartOpenBracket)
    |> orParse (closeBracketParser |> map0Parse CodeBlockPartCloseBracket)
    |> orParse (underscoreParser |> map0Parse CodeBlockPartUnderscore)
    |> orParse (asteriskParser |> map0Parse CodeBlockPartAsterisk)
    |> orParse (hashParser |> map0Parse CodeBlockPartHash)
    |> orParse (hashSpaceParser |> map0Parse CodeBlockPartHashSpace)
    |> orParse (greaterThanSpaceParser |> map0Parse CodeBlockPartGreaterThanSpace)

let private codeBlockLineParser: Parser<CodeBlockLineNode> =
    matchPlus codeBlockPartParser
    |> andParse newLineParser
    |> keepFirstParse
    |> mapParse CodeBlockLineValue

let private codeBlockParser: Parser<ParagraphNode> =
    let tripleBacktick =
        backtickParser
        |> andParse backtickParser
        |> andParse backtickParser

    let content: Parser<CodeBlockLineNode list> = matchPlus codeBlockLineParser

    tripleBacktick |> andParse newLineParser
    |> andParse content
    |> keepSecondParse
    |> andParse tripleBacktick
    |> keepFirstParse
    |> mapParse CodeBlock
    |> andParse (matchStar newLineParser)
    |> keepFirstParse

let private quoteBlockParser: Parser<ParagraphNode> =
    let quoteBlockPartParser: Parser<QuoteBlockPartNode> =
        textParser
        |> mapParse QuoteBlockPartText
        |> orParse (openParenthesesParser |> map0Parse QuoteBlockPartOpenParentheses)
        |> orParse (closeParenthesesParser |> map0Parse QuoteBlockPartCloseParentheses)
        |> orParse (openBracketParser |> map0Parse QuoteBlockPartOpenBracket)
        |> orParse (closeBracketParser |> map0Parse QuoteBlockPartCloseBracket)
        |> orParse (underscoreParser |> map0Parse QuoteBlockPartUnderscore)
        |> orParse (asteriskParser |> map0Parse QuoteBlockPartAsterisk)
        |> orParse (hashParser |> map0Parse QuoteBlockPartHash)
        |> orParse (hashSpaceParser |> map0Parse QuoteBlockPartHashSpace)
        |> orParse (backtickParser |> map0Parse QuoteBlockPartBacktick)
        |> orParse (greaterThanSpaceParser |> map0Parse QuoteBlockPartGreaterThanSpace)

    let quoteBlockLineParser: Parser<QuoteBlockLineNode> =
        greaterThanSpaceParser
        |> andParse (matchPlus quoteBlockPartParser)
        |> keepSecondParse
        |> mapParse QuoteBlockLineValue

    let subsequent: Parser<QuoteBlockLineNode> =
        newLineParser
        |> andParse quoteBlockLineParser
        |> keepSecondParse

    quoteBlockLineParser
    |> andParse (matchStar subsequent)
    |> mapParse (fun (n, nn) -> n :: nn)
    |> mapParse QuoteBlock
    |> andParse (matchStar newLineParser)
    |> keepFirstParse

let private orderedListParser: Parser<ParagraphNode> =
    let oneDot = singleTokenParser Token.OneDotSpaceSpace

    let lineParser =
        oneDot
        |> andParse (matchStar sentenceParser)
        |> keepSecondParse
        |> mapParse OrderedListLineValue

    let subsequent =
        newLineParser
        |> andParse lineParser
        |> keepSecondParse

    lineParser
    |> andParse (matchStar subsequent)
    |> mapParse (fun (n, nn) -> n :: nn)
    |> mapParse OrderedList
    |> andParse (matchStar newLineParser)
    |> keepFirstParse

let private paragraphNodeParser: Parser<ParagraphNode> =
    paragraphLinesParser
    |> orParse codeBlockParser
    |> orParse quoteBlockParser
    |> orParse paragraphHeading3Parser
    |> orParse paragraphHeading2Parser
    |> orParse paragraphHeading1Parser
    |> orParse orderedListParser

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
    | SimpleCodeHash -> "#"
    | SimpleCodeHashSpace -> "# "
    | SimpleCodeGreaterThanSpace -> "> "

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
    | HashSpace -> Span "# "

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

let private renderQuoteBlockLines (nodes: QuoteBlockLineNode list): string =
    let renderQuoteBlockPart (node: QuoteBlockPartNode): string =
        match node with
        | QuoteBlockPartText(TextValue text) -> text
        | QuoteBlockPartOpenParentheses -> "("
        | QuoteBlockPartCloseParentheses -> ")"
        | QuoteBlockPartOpenBracket -> "["
        | QuoteBlockPartCloseBracket -> "]"
        | QuoteBlockPartUnderscore -> "_"
        | QuoteBlockPartAsterisk -> "*"
        | QuoteBlockPartHash -> "#"
        | QuoteBlockPartHashSpace -> "# "
        | QuoteBlockPartBacktick -> "`"
        | QuoteBlockPartGreaterThanSpace -> "> "

    let renderQuoteBlockLine (node: QuoteBlockLineNode): string =
        match node with
        | QuoteBlockLineValue(parts) ->
            parts
            |> List.map renderQuoteBlockPart
            |> String.concat ""

    nodes
    |> List.map renderQuoteBlockLine
    |> String.concat " "

let private renderCodeBlockLines (lines: CodeBlockLineNode list): string =
    let renderCodeBlockPart (node: CodeBlockPartNode): string =
        match node with
        | CodeBlockPartTextValue(TextValue t) -> t
        | CodeBlockPartOpenParentheses -> "("
        | CodeBlockPartCloseParentheses -> ")"
        | CodeBlockPartOpenBracket -> "["
        | CodeBlockPartCloseBracket -> "]"
        | CodeBlockPartUnderscore -> "_"
        | CodeBlockPartAsterisk -> "*"
        | CodeBlockPartHash -> "#"
        | CodeBlockPartHashSpace -> "# "
        | CodeBlockPartGreaterThanSpace -> "> "

    let renderCodeBlockLine (line: CodeBlockLineNode): string =
        match line with
        | CodeBlockLineValue nodes ->
            nodes
            |> List.map renderCodeBlockPart
            |> String.concat ""

    lines
    |> List.map renderCodeBlockLine
    |> String.concat "\n"

let private renderOrderedListLine (line: OrderedListLineNode): MarkdownListItem =
    match line with
        | OrderedListLineValue parts ->
            parts
            |> List.map renderSentence
            |> MarkdownListItem.ListItem

let private renderParagraph (paragraph: ParagraphNode): MarkdownParagraph =
    match paragraph with
    | Lines(line, subsequent) ->
        let flatten = List.fold List.append []

        let spans = renderLine line @ (flatten <| List.map renderSubsequentLine subsequent)

        MarkdownParagraph.Paragraph spans
    | CodeBlock(lines) ->
        lines
        |> renderCodeBlockLines
        |> MarkdownParagraph.CodeBlock
    | QuoteBlock(lines) ->
        lines
        |> renderQuoteBlockLines
        |> MarkdownParagraph.QuoteBlock
    | Heading1(sentences) ->
        sentences
        |> List.map renderSentence
        |> MarkdownParagraph.Heading1
    | Heading2(sentences) ->
        sentences
        |> List.map renderSentence
        |> MarkdownParagraph.Heading2
    | Heading3(sentences) ->
        sentences
        |> List.map renderSentence
        |> MarkdownParagraph.Heading3
    | OrderedList(lines) ->
        lines
        |> List.map renderOrderedListLine
        |> MarkdownParagraph.OrderedList

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
