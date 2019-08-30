module MarkdownTests

open Xunit
open NoizwavesBlog.OwnMarkdown


let private debug =
    if not (System.Diagnostics.Debugger.IsAttached) then
      printfn "Please attach a debugger, PID: %d" (System.Diagnostics.Process.GetCurrentProcess().Id)
    while not (System.Diagnostics.Debugger.IsAttached) do
      System.Threading.Thread.Sleep(100)

[<Fact>]
let ``Single line paragraph``() =
    let expected: Markdown = [ Paragraph [ Span "Foo" ] ]
    let actual: Markdown = ParseOwn """Foo""" |> Option.get
    Assert.Equal<Markdown>(expected, actual)

[<Fact>]
let ``Single line paragraph with newline``() =
    let expected: Markdown = [ Paragraph [ Span "Foo" ] ]
    let actual: Markdown = Option.get <| ParseOwn """Foo
"""
    Assert.Equal<Markdown>(expected, actual)

[<Fact>]
let ``Multiple single line paragraphs``() =
    let expected: Markdown = [ Paragraph [ Span "Foo" ]; Paragraph [ Span "Bar" ] ]
    let actual: Markdown = Option.get <| ParseOwn """Foo

Bar"""
    Assert.Equal<Markdown>(expected, actual)

[<Fact>]
let ``Multiple line paragraph``() =
    let expected: Markdown =
        [ Paragraph [ Span "Foo"; Span "Bar" ] ]
    let actual: Markdown = Option.get <| ParseOwn """Foo
Bar"""
    Assert.Equal<Markdown>(expected, actual)

[<Fact>]
let ``Multiple paragraphs``() =
    let expected: Markdown =
        [ Paragraph [ Span "Foo" ]
   ; Paragraph [ Span "Bar"; Span "Baz" ]
        ]
    let actual: Markdown = Option.get <| ParseOwn """Foo

Bar
Baz"""
    Assert.Equal<Markdown>(expected, actual)

[<Fact>]
let ``Emphasized text in a paragraph``() =
    let expected: Markdown =
        [ Paragraph [ Emphasized "Foo" ] ]
    let actual: Markdown = Option.get <| ParseOwn """_Foo_"""
    Assert.Equal<Markdown>(expected, actual)

[<Fact>]
let ``Bold text in a paragraph``() =
    let expected: Markdown =
        [ Paragraph [ Bolded "Foo" ] ]
    let actual: Markdown = Option.get <| ParseOwn """**Foo**"""
    Assert.Equal<Markdown>(expected, actual)

[<Fact>]
let ``Alternatively bolded text in a paragraph``() =
    let expected: Markdown =
        [ Paragraph [ Bolded "Foo" ] ]
    let actual: Markdown = Option.get <| ParseOwn """__Foo__"""
    Assert.Equal<Markdown>(expected, actual)

[<Fact>]
let ``Mix of regular, bolded, and emphasized text in a paragraph``() =
    let expected: Markdown =
        [ Paragraph
            [ Emphasized "Foo"
       ; Span " Bar"
       ; Span "Baz "
       ; Emphasized "Qux"
       ; Bolded "Quux"
       ; Span " Quuz "
       ; Emphasized "Corge"
       ; Span " "
       ; Bolded "Grault"
            ]
        ]
    let actual: Markdown = Option.get <| ParseOwn """_Foo_ Bar
Baz _Qux_
**Quux** Quuz _Corge_ __Grault__"""
    Assert.Equal<Markdown>(expected, actual)

[<Fact>]
let ``Inline link in a paragraph``() =
    let expected: Markdown = [ Paragraph [ InlineLink("www.example.com", "foobar") ] ]
    let actual: Markdown = Option.get <| ParseOwn """[foobar](www.example.com)"""
    Assert.Equal<Markdown>(expected, actual)

[<Fact>]
let ``Inline code span using single backticks``() =
    let expected: Markdown = [ Paragraph [ Code "foobar" ] ]
    let actual: Markdown = Option.get <| ParseOwn """`foobar`"""
    Assert.Equal<Markdown>(expected, actual)

[<Fact>]
let ``Inline code span using double backticks``() =
    let expected: Markdown = [ Paragraph [ Code "baz" ] ]
    let actual: Markdown = Option.get <| ParseOwn """``baz``"""
    Assert.Equal<Markdown>(expected, actual)

[<Fact>]
let ``Inline code span containing a literal backtick``() =
    let expected: Markdown = [ Paragraph [ Code "b`az" ] ]
    let actual: Markdown = Option.get <| ParseOwn """``b`az``"""
    Assert.Equal<Markdown>(expected, actual)

[<Fact>]
let ``Inline code span containing two literal backticks``() =
    let expected: Markdown = [ Paragraph [ Code "b`a`z" ] ]
    let actual: Markdown = Option.get <| ParseOwn """``b`a`z``"""
    Assert.Equal<Markdown>(expected, actual)

[<Fact>]
let ``Inline code span containing multiple literal backticks``() =
    let expected: Markdown = [ Paragraph [ Code "b`a`z` `q`u`x" ] ]
    let actual: Markdown = Option.get <| ParseOwn """``b`a`z` `q`u`x``"""
    Assert.Equal<Markdown>(expected, actual)

[<Fact>]
let ``Inline code span containing multiple token characters``() =
    let expected: Markdown = [ Paragraph [ Code "([_*])# " ] ]
    let actual: Markdown = Option.get <| ParseOwn """`([_*])# `"""
    Assert.Equal<Markdown>(expected, actual)

[<Fact>]
let ``Inline code span containing a syntactically valid link``() =
    let expected: Markdown = [ Paragraph [ Code "[not a link](www.example.com)" ] ]
    let actual: Markdown = Option.get <| ParseOwn """`[not a link](www.example.com)`"""
    Assert.Equal<Markdown>(expected, actual)

[<Fact>]
let ``Double backticked inline code span containing multiple token characters``() =
    let expected: Markdown = [ Paragraph [ Code "([_*])# " ] ]
    let actual: Markdown = Option.get <| ParseOwn """``([_*])# ``"""
    Assert.Equal<Markdown>(expected, actual)

[<Fact>]
let ``Double backticked inline code span containing multiple token characters and escaped backticks``() =
    let expected: Markdown = [ Paragraph [ Code "([_`*`])# " ] ]
    let actual: Markdown = Option.get <| ParseOwn """``([_`*`])# ``"""
    Assert.Equal<Markdown>(expected, actual)

[<Fact>]
let ``Heading 1``() =
    let expected: Markdown = [ Heading1 [ Span "Foo" ] ]
    let actual: Markdown = Option.get <| ParseOwn """# Foo"""
    Assert.Equal<Markdown>(expected, actual)

[<Fact>]
let ``Heading 1 immediately followed by paragraph``() =
    let expected: Markdown = [ Heading1 [ Span "foo" ]; Paragraph [ Span "bar" ] ]
    let actual: Markdown = Option.get <| ParseOwn """# foo
bar
"""
    Assert.Equal<Markdown>(expected, actual)

[<Fact>]
let ``Heading 1 followed by paragraph``() =
    let expected: Markdown = [ Heading1 [ Span "foo" ]; Paragraph [ Span "bar" ] ]
    let actual: Markdown = Option.get <| ParseOwn """# foo

bar
"""
    Assert.Equal<Markdown>(expected, actual)

[<Fact>]
let ``Regular paragraph containing heading 1-like characters``() =
    let expected: Markdown = [ Paragraph [ Span "foo # Bar" ] ]
    let actual: Markdown = Option.get <| ParseOwn """foo # Bar"""
    Assert.Equal<Markdown>(expected, actual)

[<Fact>]
let ``Regular paragraph immediately followed by heading 1``() =
    let expected: Markdown = [ Paragraph [ Span "foo" ]; Heading1 [ Span "baz" ] ]
    let actual: Markdown = Option.get <| ParseOwn """foo
# baz"""
    Assert.Equal<Markdown>(expected, actual)

[<Fact>]
let ``Regular paragraph with subsequent sentence containing heading 1-like characters``() =
    let expected: Markdown = [ Paragraph [ Span "foo"; Span "bar # baz" ] ]
    let actual: Markdown = Option.get <| ParseOwn """foo
bar # baz"""
    Assert.Equal<Markdown>(expected, actual)

[<Fact>]
let ``Heading 1 that includes the hash space string``() =
    let expected: Markdown = [ Heading1 [ Span "foo # bar" ] ]
    let actual: Markdown = Option.get <| ParseOwn "# foo # bar"
    Assert.Equal<Markdown>(expected, actual)

[<Fact>]
let ``Heading 1 that includes sentence parts``() =
    let expected: Markdown = [ Heading1 [ Emphasized "foo"; Span " "; Bolded "bar"; Span " baz # h1 "; Code "Qux"; InlineLink("quuz", "quux") ] ]
    let actual: Markdown = Option.get <| ParseOwn "# _foo_ **bar** baz # h1 `Qux`[quux](quuz)"
    Assert.Equal<Markdown>(expected, actual)
