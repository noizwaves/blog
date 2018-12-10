module Program

open Suave
open Suave.DotLiquid
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.Json
open Suave.Utils
open System
open System.Collections.Generic
open System.IO
open System.Text
open FSharp.Markdown

type PostHtmlDto =
    { title : String
      createdAt : String
      bodyHtml : String }

let private shallowYamlDecode (yml : String) : Map<String, String> =
    yml
    |> String.split '\n'
    |> List.map (fun s -> 
           let index = s.IndexOf(":")
           let key = s.Substring(0, index).Trim()
           let value = s.Substring(index + 1).Trim().Trim('\"')
           (key, value))
    |> Map.ofList

let private parsePostFile (raw : String) =
    let split = raw.Split("---")
    if (split.Length >= 2) then 
        let document = Array.get split (split.Length - 1) |> String.trim
        
        let frontmatter =
            Array.get split (split.Length - 2)
            |> String.trim
            |> shallowYamlDecode
        (frontmatter, document)
    else (Map.empty, raw)

let private handleBlogPost slug =
    request (fun r -> 
        let raw = sprintf "_posts/%s.md" slug |> System.IO.File.ReadAllText
        let (frontMatter, document) = parsePostFile raw
        let title = Map.tryFind "title" frontMatter |> Option.defaultValue (String.replace "-" " " slug)
        
        let fileCreatedAt =
            sprintf "_posts/%s.md" slug
            |> System.IO.File.GetCreationTime
            |> fun d -> d.ToString()
        
        let createdAt = Map.tryFind "date" frontMatter |> Option.defaultValue fileCreatedAt
        let parsed = Markdown.Parse(document)
        
        let viewSpan s =
            match s with
            | Literal(text) -> text
            | InlineCode(code) -> sprintf "<code class=\"highlighter-rouge\">%s</code>" code
            | Strong _ -> "Strong"
            | Emphasis _ -> "Emphasis"
            | AnchorLink _ -> "AnchorLink"
            | DirectLink _ -> "DirectLink"
            | IndirectLink _ -> "IndirectLink"
            | DirectImage(body, (link, _)) -> 
                let src = link |> String.replace "{{ site.url }}" ""
                sprintf "<img src=\"%s\" alt=\"%s\" />" src body
            | IndirectImage _ -> "IndirectImage"
            | HardLineBreak _ -> "<br>"
            | LatexInlineMath _ -> "LatexInlineMath"
            | LatexDisplayMath _ -> "LatexDisplayMath"
            | EmbedSpans _ -> "EmbedSpans"
        
        let viewSpans spans = List.map viewSpan spans |> List.reduce (fun s1 s2 -> s1 + s2)
        
        let viewParagraph p =
            match p with
            | Heading(size, spans) -> sprintf "<h%i>%s</h%i>" size (viewSpans spans) size
            | Paragraph(spans) -> sprintf "<p>%s</p>" (viewSpans spans)
            | CodeBlock _ -> "CodeBlock"
            | InlineBlock _ -> "InlineBlock"
            | ListBlock _ -> "ListBlock"
            | QuotedBlock _ -> "QuotedBlock"
            | Span _ -> "Span"
            | LatexBlock _ -> "LatexBlock"
            | HorizontalRule _ -> "HorizontalRule"
            | TableBlock _ -> "TableBlock"
            | EmbedParagraphs _ -> "EmbedParagraphs"
        
        let body = List.map viewParagraph parsed.Paragraphs |> List.reduce (fun s1 s2 -> s1 + s2)
        
        let model =
            { title = title
              createdAt = createdAt
              bodyHtml = body }
        page "post.html.liquid" model)

[<EntryPoint>]
let main _ =
    let port =
        Environment.GetEnvironmentVariable "PORT"
        |> Parse.int32
        |> Choice.fold id (fun _ -> 8080)
    
    let local = Suave.Http.HttpBinding.createSimple HTTP "0.0.0.0" port
    
    let config =
        { defaultConfig with bindings = [ local ]
                             homeFolder = Some(Path.GetFullPath "./public") }
    setTemplatesDir "./templates"
    setCSharpNamingConvention()
    let app : WebPart =
        choose [ GET >=> pathScan "/posts/%s" handleBlogPost
                 GET >=> Files.browseHome
                 RequestErrors.NOT_FOUND "Page not found." ]
    startWebServer config app
    0
