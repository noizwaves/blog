module NoizwavesBlog.Markdown

open Markdig

let convertToHtml (markdown: string): string =
    Markdown.ToHtml(markdown)
