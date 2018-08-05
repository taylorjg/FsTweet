module Database

open FSharp.Data.Sql
open Npgsql

[<Literal>]
let private ConnectionString =
  "Server=localhost;Port=5432;User Id=postgres;Password=test;Database=FsTweet;"

[<Literal>]
let private ResolutionPath =
  @"./../../packages/database/Npgsql/lib/net451"

type Db = SqlDataProvider<
            ConnectionString=ConnectionString,
            DatabaseVendor=Common.DatabaseProviderTypes.POSTGRESQL,
            ResolutionPath=ResolutionPath,
            UseOptionTypes=true>
type DataContext = Db.dataContext
type GetDataContext = unit -> DataContext

let makeConnectionString databaseUrl =
  let uri = new System.Uri(databaseUrl)
  let userInfo = uri.UserInfo
  let userInfoBits = userInfo.Split([|':'|])
  let username = userInfoBits.[0]
  let password = userInfoBits.[1]
  sprintf
    "Server=%s;Port=%d;User Id=%s;Password=%s;Database=FsTweet;"
    uri.Host
    uri.Port
    username
    password
    
let dataContext (connectionString: string): GetDataContext =
  fun _ -> Db.GetDataContext connectionString
