[<RequireQualifiedAccess>]
module GetStream

open Stream

type Config = {
  ApiSecret: string
  ApiKey: string
  AppId: string
}

type Client = {
  Config: Config
  StreamClient: StreamClient
}

let newClient config = {
  Config = config
  StreamClient = new StreamClient(config.ApiKey, config.ApiSecret)
}

let userFeed getStreamClient userId =
  getStreamClient.StreamClient.Feed("user", string userId)
