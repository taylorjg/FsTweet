﻿open Suave
open Suave.DotLiquid
open Suave.Files
open Suave.Filters
open Suave.Operators
open System.IO
open System.Reflection

let currentPath =
    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)

let initDotLiquid =
    let templatesDir = Path.Combine(currentPath, "views")
    setTemplatesDir templatesDir

let serveAssets =
    pathRegex "/assets/*" >=> browseHome

let serveFavIcon =
    let favIconPath = Path.Combine(currentPath, "assets", "images", "favicon.ico")
    path "/favicon.ico" >=> file favIconPath

let app =
    choose [
        serveAssets
        serveFavIcon
        path "/" >=> page "guest/home.liquid" ""
    ]

let config =
    { defaultConfig with
        bindings = [ HttpBinding.createSimple HTTP "0.0.0.0" 5000 ]
        homeFolder = Some currentPath
    }

[<EntryPoint>]
let main argv =
    initDotLiquid
    setCSharpNamingConvention ()
    startWebServer config app
    0
