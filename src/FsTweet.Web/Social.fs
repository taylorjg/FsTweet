namespace Social

module Domain =
  open Chessie.ErrorHandling
  open System
  open User

  type CreateFollowing = User -> UserId -> AsyncResult<unit, Exception>
  type Subscribe = User -> UserId -> AsyncResult<unit, Exception>
  type FollowUser = User -> UserId -> AsyncResult<unit, Exception>
  type IsFollowing = User -> UserId -> AsyncResult<bool, Exception>

  let followUser
    (subscribe: Subscribe)
    (createFollowing: CreateFollowing)
    user
    userId = asyncTrial {

      do! subscribe user userId
      do! createFollowing user userId      
    }
 
 module Persistence = 
  open Chessie
  open Chessie.ErrorHandling
  open Database
  open Microsoft.EntityFrameworkCore
  open User

  let createFollowing (getDataContext: GetDataContext) (user: User) (UserId userId) =
    use dbContext = getDataContext ()
    let (UserId followerUserId) = user.UserId
    let social = {
      Id = System.Guid.NewGuid ()
      FollowerUserId = followerUserId
      FollowingUserId = userId
    }
    dbContext.Add(social) |> ignore
    saveChangesAsync dbContext

  let isFollowing (getDataContext: GetDataContext) (user: User) (UserId userId) = asyncTrial {
    use dbContext = getDataContext ()
    let (UserId followerUserId) = user.UserId
    let queryable = query {
      for s in dbContext.Social do
        where (s.FollowerUserId = followerUserId && s.FollowingUserId = userId)
    }
    let! maybeConnection =
      EntityFrameworkQueryableExtensions.ToListAsync(queryable)
      |> Async.AwaitTask
      |> AR.catch
      |> AR.mapSuccess (List.ofSeq >> List.tryHead)
    return maybeConnection.IsSome
  }  

module GetStream =
  open Chessie
  open User

  let subscribe (getStreamClient: GetStream.Client) (user: User) (UserId userId) =
    let (UserId followerUserId) = user.UserId
    let timelineFeed = GetStream.timelineFeed getStreamClient followerUserId
    let userFeed = GetStream.userFeed getStreamClient userId
    timelineFeed.FollowFeed(userFeed)
    |> Async.AwaitTask
    |> AR.catch

module Suave =
  open Auth
  open Chessie
  open Chiron
  open Domain
  open Persistence
  open Suave
  open Suave.Filters
  open Suave.Operators
  open User

  type FollowUserRequest = FollowUserRequest of int with
    static member FromJson (_: FollowUserRequest) = json {
      let! userId = Json.read "userId"
      return FollowUserRequest userId
    }

  let private onFollowUserSuccess () =
    Successful.NO_CONTENT

  let private onFollowUserFailure (ex: System.Exception) =
    printfn "[onFollowUserFailure] %A" ex
    JSON.internalServerError

  let private handleFollowUser (followUser: FollowUser) (user: User) ctx = async {
    match JSON.deserialise ctx.request with
    | Success (FollowUserRequest userId) ->
      let! webpart =
        followUser user (UserId userId)
        |> AR.either onFollowUserSuccess onFollowUserFailure
      return! webpart ctx
    | Failure _ ->
      return! JSON.badRequest "Invalid follow user request" ctx
  }

  let webpart getDataContext getStreamClient =
    let createFollowing = createFollowing getDataContext
    let subscribe = GetStream.subscribe getStreamClient
    let followUser = followUser subscribe createFollowing
    let handleFollowUser = handleFollowUser followUser
    POST >=> path "/follow" >=> requiresAuth2 handleFollowUser
