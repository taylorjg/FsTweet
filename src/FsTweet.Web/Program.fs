open Database
open Suave
open Suave.DotLiquid
open Suave.Files
open Suave.Filters
open Suave.Operators
open System
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

let portEnvVar = Environment.GetEnvironmentVariable "PORT"
let port = if String.IsNullOrEmpty portEnvVar then 5000 else (int)portEnvVar

let config =
  { defaultConfig with
      bindings = [ HttpBinding.createSimple HTTP "0.0.0.0" port ]
      homeFolder = Some currentPath
  }

[<EntryPoint>]
let main argv =

  let databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL")
  let connectionString = makeConnectionString databaseUrl
  let getDataContext = dataContext connectionString

  let app =
    choose [
      serveAssets
      serveFavIcon
      path "/" >=> page "guest/home.liquid" ""
      UserSignup.Suave.webPart getDataContext
    ]

  printfn "connectionString: %s" connectionString
  initDotLiquid
  setCSharpNamingConvention ()
  startWebServer config app
  0
