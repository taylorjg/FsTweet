#load ".fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators

let noFilter = fun _ -> true

Target.create "Views" (fun _ ->
    let srcDir = "./src/views"
    let targetDir1 = "./src/bin/Debug/netcoreapp2.1/views"
    let targetDir2 = "./src/bin/Release/netcoreapp2.1/views"
    Shell.copyDir targetDir1 srcDir noFilter
    Shell.copyDir targetDir2 srcDir noFilter
)

Target.create "Clean" (fun _ ->
    !! "src/**/bin"
    ++ "src/**/obj"
    |> Shell.cleanDirs 
)

Target.create "Build" (fun _ ->
    !! "src/**/*.*proj"
    |> Seq.iter (DotNet.build id)
)

Target.create "All" ignore

"Clean"
  ==> "Build"
  ==> "Views"
  ==> "All"

Target.runOrDefault "All"
