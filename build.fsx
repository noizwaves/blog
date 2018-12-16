#r "paket:
nuget Fake.Core.Target
nuget Fake.IO.FileSystem
nuget Fake.DotNet.Cli //"

open Fake.Core
open Fake.DotNet
open Fake.IO

exception BuildException

let private ensureSuccessExitCode (exitCode : int) =
    match exitCode with
    | 0 -> ()
    | _ -> raise BuildException

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

Target.create "Publish" (fun _ ->
    let result = DotNet.exec id "publish" "-c Release"
    ensureSuccessExitCode result.ExitCode
)

// *** Define Dependencies ***
open Fake.Core.TargetOperators

let dependsOn (tasks : string list) (task : string) = task <== tasks
let mustRunAfter (otherTask : string) (task : string) = task <=? otherTask |> ignore

"Build" |> mustRunAfter "Clean"

"Publish" |> dependsOn [ "Clean" ; "Build" ]

// *** Start Build ***
Target.runOrDefault "Build"

