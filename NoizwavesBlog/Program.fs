module Program

open NoizwavesBlog
open System

[<EntryPoint>]
let main args =
    Environment.GetEnvironmentVariable "PORT" |> printfn "Program.fs: %O"
    let staticMode = Array.contains "static" args
    if staticMode then StaticExport.run
    else WebServer.run
