#load ".fake/build.fsx/intellisense.fsx"

open System.IO
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators

let distDir = Path.Combine(Directory.GetCurrentDirectory(), "dist")

Target.create "Views" (fun _ ->
    let srcDir = "./src/views"
    let targetDir = Path.Combine(distDir, "views") 
    let noFilter = fun _ -> true
    Shell.copyDir targetDir srcDir noFilter
)

Target.create "Clean" (fun _ ->
    !! "src/**/bin"
    ++ "src/**/obj"
    |> Shell.cleanDirs 
)

let setOutputPath buildOptions: DotNet.BuildOptions =
    { buildOptions with OutputPath = Some distDir }

Target.create "Build" (fun _ ->
    !! "src/**/*.*proj"
    |> Seq.iter (DotNet.build setOutputPath)
)

Target.create "Run" (fun _ ->
    let cmd = Path.Combine(distDir, "FsTweet.dll")
    DotNet.exec id cmd "" |> ignore
)

Target.create "All" ignore

"Clean"
  ==> "Build"
  ==> "Views"
  ==> "All"

Target.runOrDefault "All"
