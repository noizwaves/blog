module MarkdownTests

open Xunit
open NoizwavesBlog.OwnMarkdown

[<Fact>]
let ``Single line paragraph`` () =
    let expected : Markdown = [ Paragraph [ Span "Foo" ] ]
    let actual : Markdown = ParseOwn """Foo""" |> Option.get
    Assert.Equal<Markdown> (expected, actual)

// [<Fact>]
// let ``Multiple line paragraph`` () =
//     let expected : Markdown = [ Paragraph [ Span "Foo"; Span "Bar"; Span "Baz" ] ]
//     let actual : Markdown = ParseOwn """Foo
// Bar
// Baz"""
//                             |> Option.get
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