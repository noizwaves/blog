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

// Domain
type CreateDate = CreateDate of System.DateTime

let parseFromLongString (value : String) : CreateDate option =
    let date = System.DateTime.Parse(value)
    Some <| CreateDate date

type BlogPost =
    { slug : String
      title : String
      createdAt : CreateDate
      body : String }

type FetchPosts = unit -> BlogPost list

type FetchPost = string -> BlogPost option

// Disk serialisation
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

let private loadPostsFromFolder (folder : String) : BlogPost list =
    folder
    |> System.IO.Directory.GetFiles
    |> Array.toList
    |> List.map (fun path -> 
           let slug = System.IO.Path.GetFileNameWithoutExtension path
           path
           |> System.IO.File.ReadAllText
           |> fromRawString slug)

let private safeFind predicate list =
    try 
        list
        |> List.find predicate
        |> Some
    with :? System.Collections.Generic.KeyNotFoundException -> None

let private findPostInList (posts : BlogPost list) (slug : String) : BlogPost option =
    posts |> safeFind (fun p -> p.slug.Equals(slug))

// HTML, Markdown formatting
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
    | CodeBlock(code, _, _) -> sprintf """<div class="highlighter-rouge"><div class="highlight"><pre class="highlight"><code>%s</code></pre></div></div>""" code
    | InlineBlock _ -> failwith "InlineBlock not translated yet"
    | ListBlock _ -> failwith "ListBlock not translated yet"
    | QuotedBlock _ -> failwith "QuotedBlock not translated yet"
    | Span _ -> failwith "Span not translated yet"
    | LatexBlock _ -> failwith "LatexBlock not translated yet"
    | HorizontalRule _ -> failwith "HorizontalRule not translated yet"
    | TableBlock _ -> failwith "TableBlock not translated yet"
    | EmbedParagraphs _ -> failwith "EmbedParagraphs not translated yet"

let private toHtmlString (document : MarkdownDocument) : String =
    document.Paragraphs
    |> List.map viewParagraph
    |> List.reduce (fun s1 s2 -> s1 + s2)

// HTML
type PostHtmlDto =
    { title : String
      createdAt : String
      bodyHtml : String }

type PostItemHtmlDto =
    { title : String
      createdAt : String
      link : String }

type PostsHtmlDto =
    { posts : PostItemHtmlDto list }

let formatCreateDate (CreateDate value) : String = value.ToString("MMM d, yyyy")

let private derivePostUrl (post : BlogPost) : String =
    match String.split '-' post.slug with
    | (year :: month :: day :: rest) -> sprintf "/%s/%s/%s/%s" year month day (String.concat "-" rest)
    | _ -> failwith "Invalid filename"

// Flows
let private handleBlogPost (fetch : FetchPost) (year, month, day, titleSlug) =
    request (fun r -> 
        let slug = sprintf "%04i-%02i-%02i-%s" year month day titleSlug
        let post = fetch slug
        
        let toDto (post : BlogPost) : PostHtmlDto =
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

let private handleBlogPosts (fetch : FetchPosts) request =
    let posts =
        fetch()
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
    let posts = loadPostsFromFolder "_posts"
    let fetchPosts = fun () -> posts
    let fetchPost = findPostInList posts
    
    let app : WebPart =
        choose [ GET >=> path "/" >=> request (handleBlogPosts fetchPosts)
                 GET >=> pathScan "/%i/%i/%i/%s" (handleBlogPost fetchPost)
                 GET >=> Files.browseHome
                 RequestErrors.NOT_FOUND "Page not found." ]
    startWebServer config app
    0
