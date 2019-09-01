module NoizwavesBlog.Markdown

open NoizwavesBlog.OwnMarkdown

let viewElement (elements: MarkdownElement): string =
    match elements with
    | Span(text) -> text
    | Emphasized(text) -> sprintf "<em>%s</em>" text
    | Bolded(text) -> sprintf "<strong>%s</strong>" text
    | InlineLink(url, text) -> sprintf """<a href="%s">%s</a>""" url text
    | Code(code) -> sprintf "<code class=\"highlighter-rouge\">%s</code>" code
    | Image(url, alt) ->
        let src = url |> String.replace "{{ site.url }}" ""
        sprintf "<img src=\"%s\" alt=\"%s\" />" src alt

let viewParagraph (paragraph: MarkdownParagraph): string =
    let viewElements (elements: MarkdownElement list): string =
        elements
        |> List.map viewElement
        |> String.concat ""

    let viewListItem (ListItem elements): string =
        viewElements elements

    match paragraph with
    | Heading1(elements) ->
        elements
        |> viewElements
        |> sprintf "<h1>%s</h1>"
    | Heading2(elements) ->
        elements
        |> viewElements
        |> sprintf "<h2>%s</h2>"
    | Heading3(elements) ->
        elements
        |> viewElements
        |> sprintf "<h3>%s</h3>"
    | Paragraph(lines) ->
        lines
        |> List.map viewElements
        |> String.concat "\n"
        |> sprintf "<p>%s</p>"
    | CodeBlock(code, _) ->
        sprintf """<div class="highlighter-rouge"><div class="highlight"><pre class="highlight"><code>%s</code></pre></div></div>""" code
    | QuoteBlock(text) ->
        sprintf "<blockquote>%s</blockquote>" text
    | OrderedList(items) ->
        sprintf "%O" items |> ignore
        items
        |> List.map viewListItem
        |> List.map (sprintf "<li>%s</li>")
        |> String.concat ""
        |> sprintf "<ol>%s</ol>"
    | UnorderedList(items) ->
        items
        |> List.map viewListItem
        |> List.map (sprintf "<li>%s</li>")
        |> String.concat ""
        |> sprintf "<ul>%s</ul>"

let toHtmlString (markdown: Markdown): string =
    markdown
    |> List.map viewParagraph
    |> String.concat ""

let convertToHtml (markdown: string) : string =
    markdown
    |> ParseOwn
    |> Option.get
    |> toHtmlString