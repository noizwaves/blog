module Program

open Suave
open Suave.DotLiquid
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.RequestErrors
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

let private viewSpan (s : MarkdownSpan) =
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

let private viewSpans (spans : MarkdownSpans) =
    spans
    |> List.map viewSpan
    |> List.reduce (fun s1 s2 -> s1 + s2)

let private viewParagraph p =
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

let private toHtmlString (document : MarkdownDocument) : String =
    document.Paragraphs
    |> List.map viewParagraph
    |> List.reduce (fun s1 s2 -> s1 + s2)

type CreateDate = CreateDate of System.DateTime

let parseFromLongString (value : String) : CreateDate option =
    let date = System.DateTime.Parse(value)
    Some <| CreateDate date

let formatCreateDate (CreateDate value) : String = value.ToString("MMM d, yyyy")

type BlogPost =
    { slug : String
      title : String
      createdAt : CreateDate
      body : String }

let private fromRawString (slug : String) (raw : String) : BlogPost =
    let (frontMatter, body) = parsePostFile raw
    let title = Map.tryFind "title" frontMatter |> Option.defaultValue (String.replace "-" " " slug)
    
    // Replace this with slug derived date as fallback
    let fileCreatedAt =
        sprintf "_posts/%s.md" slug
        |> System.IO.File.GetCreationTime
        |> CreateDate
    
    let createdAt =
        Map.tryFind "date" frontMatter
        |> Option.bind parseFromLongString
        |> Option.defaultValue fileCreatedAt
    
    { slug = slug
      title = title
      createdAt = createdAt
      body = body }

let private handleBlogPost slug =
    request (fun r -> 
        let post =
            sprintf "_posts/%s.md" slug
            |> System.IO.File.ReadAllText
            |> fromRawString slug
        
        let parsed = Markdown.Parse(post.body)
        let htmlBody = toHtmlString parsed
        
        let model =
            { title = post.title
              createdAt = post.createdAt |> formatCreateDate
              bodyHtml = htmlBody }
        page "post.html.liquid" model)

let private handleBlogPostPrecise (year, month, day, titleSlug) =
    request (fun r -> 
        let allPosts =
            System.IO.Directory.GetFiles "_posts"
            |> Array.toList
            |> List.map (fun path -> 
                   let slug = System.IO.Path.GetFileNameWithoutExtension path
                   path
                   |> System.IO.File.ReadAllText
                   |> fromRawString slug)
        
        let slug = sprintf "%04i-%02i-%02i-%s" year month day titleSlug
        
        let safeFind predicate list =
            try 
                list
                |> List.find predicate
                |> Some
            with :? System.Collections.Generic.KeyNotFoundException -> None
        
        let post = allPosts |> safeFind (fun p -> p.slug.Equals(slug))
        
        let toDto post : PostHtmlDto =
            { title = post.title
              createdAt = post.createdAt |> formatCreateDate
              bodyHtml =
                  post.body
                  |> Markdown.Parse
                  |> toHtmlString }
        
        let dto : PostHtmlDto option = post |> Option.map toDto
        match dto with
        | Some dto -> page "post.html.liquid" dto
        | None -> NOT_FOUND "404")

type PostItemHtmlDto =
    { title : String
      createdAt : String
      link : String }

type PostsHtmlDto =
    { posts : PostItemHtmlDto list }

let private derivePostUrl (post : BlogPost) : String =
    match String.split '-' post.slug with
    | (year :: month :: day :: rest) -> sprintf "/%s/%s/%s/%s" year month day (String.concat "-" rest)
    | _ -> failwith "Invalid filename"

let private handleBlogPosts request =
    let posts =
        System.IO.Directory.GetFiles "_posts"
        |> Array.toList
        |> List.map (fun path -> 
               let slug = System.IO.Path.GetFileNameWithoutExtension path
               path
               |> System.IO.File.ReadAllText
               |> fromRawString slug)
        |> List.sortByDescending (fun p -> p.createdAt)
        |> List.map (fun post -> 
               { title = post.title
                 createdAt = post.createdAt |> formatCreateDate
                 link = post |> derivePostUrl })
    
    let model = { posts = posts }
    page "posts.html.liquid" model

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
        choose [ GET >=> path "/" >=> request handleBlogPosts
                 GET >=> pathScan "/%i/%i/%i/%s" handleBlogPostPrecise
                 GET >=> Files.browseHome
                 RequestErrors.NOT_FOUND "Page not found." ]
    startWebServer config app
    0
