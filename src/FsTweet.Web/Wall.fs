namespace Wall

module Suave =
  open Auth.Suave
  open Chessie
  open Chessie.ErrorHandling
  open Chiron
  open Suave
  open Suave.DotLiquid
  open Suave.Filters
  open Suave.Operators
  open Tweet
  open User
  open UserSignup

  type WallViewModel = {
    Username: string
  }

  type PostRequest = PostRequest of string with
    static member FromJson (_: PostRequest) = json {
      let! post = Json.read "post"
      return PostRequest post
    }

  let private renderWall (user: User) ctx = async {
    let viewModel = { Username = user.Username.Value }
    return! page "user/wall.liquid" viewModel ctx
  }

  let private onCreateTweetSuccess (TweetId id): WebPart =
    ["id", String (id.ToString())]
    |> Map.ofList
    |> Object
    |> JSON.ok

  let private onCreateTweetFailure (ex: System.Exception): WebPart =
    printfn "[onCreateTweetFailure] %A" ex
    JSON.internalServerError

  let private handleNewTweet createTweet (user: User) ctx = async {
    match JSON.deserialise ctx.request with
    | Success (PostRequest post) ->
      match Post.TryCreate post with
      | Success post ->
        let! webpart =
          createTweet user.UserId post
          |> AR.either onCreateTweetSuccess onCreateTweetFailure
        return! webpart ctx
      | Failure msg -> return! JSON.badRequest msg ctx
    | Failure msg -> return! JSON.badRequest msg ctx
  }

  let webpart getDataContext =
    let createTweet = Persistence.createTweet getDataContext
    choose [
      GET >=> path "/wall" >=> requiresAuth renderWall
      POST >=> path "/tweets" >=> requiresAuth2 (handleNewTweet createTweet)
    ]
