module NoizwavesBlog.Markdown

open Markdig

let convertToHtml (markdown: string): string =
    markdown |> Markdown.ToHtml

let convertToText (markdown: string): string =
    markdown |> Markdown.ToPlainText
