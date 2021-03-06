﻿open Database
open Email
open Hopac
open Logary
open Logary.Configuration
open Logary.Targets
open Suave
open Suave.DotLiquid
open Suave.Files
open Suave.Filters
open Suave.Operators
open System
open System.IO
open System.Reflection
open Aether

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
let port = if String.IsNullOrEmpty portEnvVar then 5000 else int portEnvVar
let databaseUrl = Environment.GetEnvironmentVariable "DATABASE_URL"
let connectionString = makeConnectionString databaseUrl
let getDataContext = dataContext connectionString
let postmarkServerKey = Environment.GetEnvironmentVariable "FSTWEET_POSTMARK_SERVER_KEY"
let senderEmailAddress = Environment.GetEnvironmentVariable "FSTWEET_SENDER_EMAIL_ADDRESS"
let siteBaseUrl = Environment.GetEnvironmentVariable "FSTWEET_SITE_BASE_URL"
let env = Environment.GetEnvironmentVariable "FSTWEET_ENVIRONMENT"
let suaveServerKey = Environment.GetEnvironmentVariable"FSTWEET_SUAVE_SERVER_KEY" |> ServerKey.fromBase64

let streamConfig: GetStream.Config = {
  ApiKey = Environment.GetEnvironmentVariable "FSTWEET_STREAM_API_KEY"
  ApiSecret = Environment.GetEnvironmentVariable "FSTWEET_STREAM_API_SECRET"
  AppId = Environment.GetEnvironmentVariable "FSTWEET_STREAM_APPID"
}

let getStreamClient = GetStream.newClient streamConfig

let config =
  { defaultConfig with
      bindings = [ HttpBinding.createSimple HTTP "0.0.0.0" port ]
      homeFolder = Some currentPath
      serverKey = suaveServerKey
  }

let sendEmail =
  match env with
  | "prod" -> initSendEmail senderEmailAddress siteBaseUrl postmarkServerKey
  | _ -> consoleSendEmail

let target = withTarget (Console.create Console.empty "console")

let rule = withRule (Rule.createForTarget "console")

let logaryConf = target >> rule

let private readUserState ctx key: 'value option =
  ctx.userState
  |> Map.tryFind key
  |> Option.map (fun x -> x :?> 'value)

let private logIfError (logger: Logger) ctx =
  readUserState ctx "err"
  |> Option.iter logger.logSimple
  succeed

let app =
  choose [
    serveAssets
    serveFavIcon
    path "/" >=> page "guest/home.liquid" ""
    UserSignup.Suave.webPart getDataContext sendEmail
    Auth.Suave.webpart getDataContext
    Wall.Suave.webpart getDataContext getStreamClient
    Social.Suave.webpart getDataContext getStreamClient
    UserProfile.Suave.webpart getDataContext getStreamClient
  ]

[<EntryPoint>]
let main argv =
  let logary = withLogaryManager "FsTweet.Web" logaryConf |> run
  let logger = logary.getLogger (PointName [|"Suave"|])
  let appWithLogger = app >=> context (logIfError logger)  
  initDotLiquid
  setCSharpNamingConvention ()
  startWebServer config appWithLogger
  0
