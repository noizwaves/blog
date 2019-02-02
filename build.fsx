#r "paket:
nuget Fake.Core.Target
nuget Fake.IO.FileSystem
nuget Fake.DotNet.Cli
nuget SharpScss //"

open Fake.Core
open Fake.DotNet
open Fake.IO
open SharpScss
open System.IO

exception BuildException

let private ensureSuccessExitCode (exitCode : int) =
    match exitCode with
    | 0 -> ()
    | _ -> raise BuildException

let private writeToFile (filePath : string) (content : string) =
    Directory.GetParent(filePath).Create()

    use writer = File.CreateText filePath
    writer.WriteLine content

// *** Define Targets ***
Target.create "Clean" (fun _ ->
    // let result = DotNet.exec id "clean" ""
    // ensureSuccessExitCode result.ExitCode
    Shell.cleanDir "./bin"
    Shell.cleanDir "./obj"
)

Target.create "Build" (fun _ ->
    let result = DotNet.exec id "build" ""
    ensureSuccessExitCode result.ExitCode
)

Target.create "CleanCss" (fun _ ->
    File.delete "public/static/app.css"
)

Target.create "BuildCss" (fun _ ->
    let srcPath = "style/src/app.scss"
    let destPath = "public/static/app.css"

    let scssOptions = new ScssOptions (OutputStyle = ScssOutputStyle.Compressed)
    let result = Scss.ConvertFileToCss (srcPath, scssOptions)

    result.Css |> writeToFile destPath
)

Target.create "Publish" (fun _ ->
    let result = DotNet.exec id "publish" "-c Release"
    ensureSuccessExitCode result.ExitCode
)

// *** Define Dependencies ***
open Fake.Core.TargetOperators

let dependsOn (tasks : string list) (task : string) = task <== tasks
let mustRunAfter (otherTask : string) (task : string) = task <=? otherTask |> ignore

"Build" |> mustRunAfter "Clean"

"BuildCss" |> mustRunAfter "CleanCss"

"Build" |> dependsOn [ "CleanCss" ; "BuildCss" ]

"Publish" |> dependsOn [ "Clean" ; "Build" ]

// *** Start Build ***
Target.runOrDefault "Build"

