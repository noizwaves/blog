module NoizwavesBlog.Markdown

open System
open FSharp.Markdown

let rec private viewSpan (s : MarkdownSpan) =
    match s with
    | Literal(text) -> text
    | InlineCode(code) -> sprintf "<code class=\"highlighter-rouge\">%s</code>" code
    | Strong(span) -> sprintf "<strong>%s</strong>" (viewSpans span)
    | Emphasis _ -> failwith "Emphasis not translated yet"
    | AnchorLink _ -> failwith "AnchorLink not translated yet"
    | DirectLink(body, (link, _)) -> sprintf """<a href="%s">%s</a>""" link (viewSpans body)
    | IndirectLink _ -> failwith "IndirectLink not translated yet"
    | DirectImage(body, (link, _)) -> 
        let src = link |> String.replace "{{ site.url }}" ""
        sprintf "<img src=\"%s\" alt=\"%s\" />" src body
    | IndirectImage _ -> failwith "IndirectImage not translated yet"
    | HardLineBreak _ -> "<br>"
    | LatexInlineMath _ -> failwith "LatexInlineMath not translated yet"
    | LatexDisplayMath _ -> failwith "LatexDisplayMath not translated yet"
    | EmbedSpans _ -> failwith "EmbedSpans not translated yet"

and private viewSpans (spans : MarkdownSpans) =
    spans
    |> List.map viewSpan
    |> List.reduce (fun s1 s2 -> s1 + s2)

let private viewParagraph p =
    match p with
    | Heading(size, spans) -> sprintf "<h%i>%s</h%i>" size (viewSpans spans) size
    | Paragraph(spans) -> sprintf "<p>%s</p>" (viewSpans spans)
    | CodeBlock(code, _, _) -> 
        sprintf 
            """<div class="highlighter-rouge"><div class="highlight"><pre class="highlight"><code>%s</code></pre></div></div>""" 
            code
    | InlineBlock _ -> failwith "InlineBlock not translated yet"
    | ListBlock _ -> failwith "ListBlock not translated yet"
    | QuotedBlock _ -> failwith "QuotedBlock not translated yet"
    | Span _ -> failwith "Span not translated yet"
    | LatexBlock _ -> failwith "LatexBlock not translated yet"
    | HorizontalRule _ -> failwith "HorizontalRule not translated yet"
    | TableBlock _ -> failwith "TableBlock not translated yet"
    | EmbedParagraphs _ -> failwith "EmbedParagraphs not translated yet"

let private toHtmlString (document : MarkdownDocument) : string =
    document.Paragraphs
    |> List.map viewParagraph
    |> List.reduce (fun s1 s2 -> s1 + s2)

let convertToHtml (markdown : String) : String =
    markdown
    |> Markdown.Parse
    |> toHtmlString
