module Program

open NoizwavesBlog

[<EntryPoint>]
let main args =
    let staticMode = Array.contains "static" args

    if staticMode then
        StaticExport.run
    else
        WebServer.run
