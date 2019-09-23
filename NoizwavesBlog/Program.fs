module Program

open NoizwavesBlog
open Suave
open Suave.DotLiquid
open Suave.Filters
open Suave.Operators
open Suave.Utils
open System
open System.IO

[<EntryPoint>]
let main _ =
    let port =
        Environment.GetEnvironmentVariable "PORT"
        |> Parse.int32
        |> Choice.fold id (fun _ -> 8080)

    let showDrafts =
        Environment.GetEnvironmentVariable "DRAFTS"
        |> fun drafts -> drafts <> "" && drafts <> null

    let local = Suave.Http.HttpBinding.createSimple HTTP "0.0.0.0" port
    
    let config =
        { defaultConfig with bindings = [ local ]
                             homeFolder = Some(Path.GetFullPath "./public") }
    setTemplatesDir "./templates"
    setCSharpNamingConvention()
    let published = Persistence.loadPostsFromFolder "_posts"
    let drafts = Persistence.loadDraftsFromFolder "_drafts"
    let posts = if showDrafts then published @ drafts else published
    let fetchPosts = fun () -> posts
    let fetchPost = Persistence.findPostInList posts
    let pages = Persistence.loadPagesFromFolder "_pages"
    let fetchPage = Persistence.findPageInList pages
    
    let app : WebPart =
        choose [ GET >=> path "/" >=> request (Html.handleBlogPosts fetchPosts pages)
                 GET >=> pathScan "/%s/%s/%s/%s.html" (Html.handleBlogPost fetchPost pages)
                 GET >=> pathScan "/%s/%s/%s/%s" (Html.handleBlogPost fetchPost pages)
                 GET >=> pathScan "/pages/%s" (Html.handlePage fetchPage pages)
                 GET >=> path "/feed.atom" >=> Atom.handleAtomFeed fetchPosts
                 GET >=> Files.browseHome
                 RequestErrors.NOT_FOUND "404" ]
    startWebServer config app
    0
