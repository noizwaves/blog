module Program

open NoizwavesBlog

[<EntryPoint>]
let main args =
    let staticMode = Array.contains "static" args

    if staticMode then
        printfn "Before StaticExport.run"
        let res = StaticExport.run
        printfn "After StaticExport.run"
        res
    else
        printfn "Before WebServer.run"
        let res = WebServer.run
        printfn "After WebServer.run"
        res
