﻿module Program

open NoizwavesBlog
open Suave
open Suave.DotLiquid
open Suave.Filters
open Suave.Operators
open Suave.RequestErrors
open Suave.Utils
open System
open System.IO

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
    let posts = Persistence.loadPostsFromFolder "_posts"
    let fetchPosts = fun () -> posts
    let fetchPost = Persistence.findPostInList posts
    
    let app : WebPart =
        choose [ GET >=> path "/" >=> request (Html.handleBlogPosts fetchPosts)
                 GET >=> pathScan "/%s/%s/%s/%s" (Html.handleBlogPost fetchPost)
                 GET >=> Files.browseHome
                 RequestErrors.NOT_FOUND "404" ]
    startWebServer config app
    0
