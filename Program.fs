module Program

open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.Json
open Suave.Utils
open System
open System.IO
open System.Text
open FSharp.Markdown

let private handleBlogPost slug =
    request (fun r -> 
        let document = sprintf "_posts/%s.md" slug |> System.IO.File.ReadAllText
        let parsed = Markdown.Parse(document)
        let html = Markdown.WriteHtml(parsed)
        OK html)

[<EntryPoint>]
let main _ =
    let port =
        Environment.GetEnvironmentVariable "PORT"
        |> Parse.int32
        |> Choice.fold id (fun _ -> 8080)
    
    let local = Suave.Http.HttpBinding.createSimple HTTP "0.0.0.0" port
    let config = { defaultConfig with bindings = [ local ] }
    
    let app : WebPart =
        choose [ GET >=> pathScan "/posts/%s" handleBlogPost
                 RequestErrors.NOT_FOUND "Page not found." ]
    startWebServer config app
    0
