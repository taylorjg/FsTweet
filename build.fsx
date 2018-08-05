#load ".fake/build.fsx/intellisense.fsx"

open System.IO
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators

let distDir = Path.Combine(Directory.GetCurrentDirectory(), "dist")

let noFilter = fun _ -> true

Target.create "Views" (fun _ ->
  let srcDir = "./src/FsTweet.Web/views"
  let targetDir = Path.Combine(distDir, "views") 
  Shell.copyDir targetDir srcDir noFilter
)

Target.create "Assets" (fun _ ->
  let srcDir = "./src/FsTweet.Web/assets"
  let targetDir = Path.Combine(distDir, "assets") 
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
  !! "src/FsTweet.Web/*.fsproj"
  |> Seq.iter (DotNet.build setOutputPath)
)

Target.create "Run" (fun _ ->
  let cmd = Path.Combine(distDir, "FsTweet.Web.dll")
  DotNet.exec id cmd "" |> ignore
)

Target.create "All" ignore

"Clean"
  ==> "Build"
  ==> "Views"
  ==> "Assets"
  ==> "All"

Target.runOrDefault "All"
