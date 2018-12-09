module Program

open Suave
open Suave.DotLiquid
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.Json
open Suave.Utils
open System
open System.IO
open System.Text
open FSharp.Markdown

        
type PostHtmlDto = 
    { title : String
      createdAt : String
      editedAt : String
      bodyHtml : String }

let private handleBlogPost slug =
    request (fun r -> 
        let title = String.replace "-" " " slug

        let createdAt =
            sprintf "_posts/%s.md" slug
            |> System.IO.File.GetCreationTime
            |> fun d -> d.ToString()

        let editedAt =
            sprintf "_posts/%s.md" slug
            |> System.IO.File.GetLastWriteTime
            |> fun d -> d.ToString()

        let document = sprintf "_posts/%s.md" slug |> System.IO.File.ReadAllText
        let parsed = Markdown.Parse(document)

        let viewSpan s =
            match s with
            | Literal(text) -> text
            | InlineCode _ -> "InlineCode"
            | Strong _ -> "Strong"
            | Emphasis _ -> "Emphasis"
            | AnchorLink _ -> "AnchorLink"
            | DirectLink _ -> "DirectLink"
            | IndirectLink _ -> "IndirectLink"
            | DirectImage _ -> "DirectImage"
            | IndirectImage _ -> "IndirectImage"
            | HardLineBreak _ -> "HardLineBreak"
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
        
        let model = { title = title
                      createdAt = createdAt
                      editedAt = editedAt
                      bodyHtml = body }
        
        page "post.html.liquid" model)

[<EntryPoint>]
let main _ =
    let port =
        Environment.GetEnvironmentVariable "PORT"
        |> Parse.int32
        |> Choice.fold id (fun _ -> 8080)
    
    let local = Suave.Http.HttpBinding.createSimple HTTP "0.0.0.0" port
    let config = { defaultConfig with bindings = [ local ] }
    
    setTemplatesDir "./templates"
    setCSharpNamingConvention ()
    
    let app : WebPart =
        choose [ GET >=> pathScan "/posts/%s" handleBlogPost
                 RequestErrors.NOT_FOUND "Page not found." ]
    startWebServer config app
    0
