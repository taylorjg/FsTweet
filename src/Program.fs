open Suave
open Suave.Filters
open Suave.Operators
open Suave.DotLiquid
open System.IO
open System.Reflection

let currentPath =
    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)

let initDotLiquid =
    let templatesDir = Path.Combine(currentPath, "views")
    setTemplatesDir templatesDir

let app =
    path "/" >=> page "guest/home.liquid" ""

let config =
    { defaultConfig with bindings = [ HttpBinding.createSimple HTTP "0.0.0.0" 5000 ] }

[<EntryPoint>]
let main argv =
    initDotLiquid
    setCSharpNamingConvention ()
    startWebServer config app
    0
