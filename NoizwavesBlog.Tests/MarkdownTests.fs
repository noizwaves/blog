module MarkdownTests

open Xunit
open NoizwavesBlog.OwnMarkdown


let private debug =
    if not(System.Diagnostics.Debugger.IsAttached) then
      printfn "Please attach a debugger, PID: %d" (System.Diagnostics.Process.GetCurrentProcess().Id)
    while not(System.Diagnostics.Debugger.IsAttached) do
      System.Threading.Thread.Sleep(100)
    System.Diagnostics.Debugger.Break()

[<Fact>]
let ``Single line paragraph`` () =
    let expected : Markdown = [ Paragraph [ Span "Foo" ] ]
    let actual : Markdown = ParseOwn """Foo""" |> Option.get
    Assert.Equal<Markdown> (expected, actual)

[<Fact>]
let ``Single line paragraph with newline`` () =
    let expected : Markdown = [ Paragraph [ Span "Foo" ] ]
    let actual : Markdown = Option.get <| ParseOwn """Foo
"""
    Assert.Equal<Markdown> (expected, actual)

[<Fact>]
let ``Multiple single line paragraphs`` () =
    let expected : Markdown = [ Paragraph [ Span "Foo" ]; Paragraph [ Span "Bar" ] ]
    let actual : Markdown = Option.get <| ParseOwn """Foo

Bar"""
    Assert.Equal<Markdown> (expected, actual)

// [<Fact>]
// let ``Multiple line paragraph`` () =
//     let expected : Markdown =
//         [ Paragraph [ Span "Foo"; Span "Bar" ] ]
//     let actual : Markdown = Option.get <| ParseOwn """Foo
// Bar"""
//     Assert.Equal<Markdown> (expected, actual)

// [<Fact>]
// let ``Multiple paragraphs`` () =
//     let expected : Markdown =
//         [ Paragraph [ Span "Foo" ]
//         ; Paragraph [ Span "Bar"; Span "Baz" ]
//         ]
//     let actual : Markdown = ParseOwn """Foo

// Bar
// Baz"""
//     Assert.Equal<Markdown> (expected, actual)