namespace Auth

open UserSignup
module Domain =
  open Chessie
  open Chessie.ErrorHandling
  open User

  type LoginRequest = {
    Username: Username
    Password: Password
  } with
    static member TryCreate (username, password) =
      trial {
        let! username = Username.TryCreate username
        let! password = Password.TryCreate password
        return {
          Username = username
          Password = password
        }
      }

  type LoginError =
  | InvalidUsernameOrPassword
  | EmailNotVerified
  | Error of System.Exception    

  type Login = FindUser -> LoginRequest -> AsyncResult<User, LoginError>    

  let login (findUser: FindUser) (loginRequest: LoginRequest) = asyncTrial {
    let! maybeUser = findUser loginRequest.Username |> AR.mapFailure Error
    match maybeUser with
    | None -> return! InvalidUsernameOrPassword |> AR.fail
    | Some user ->
      match user.UserEmailAddress with
      | NotVerified _ -> return! EmailNotVerified |> AR.fail
      | Verified _ ->
        match PasswordHash.VerifyPassword loginRequest.Password user.PasswordHash with
        | false -> return! InvalidUsernameOrPassword |> AR.fail
        | true -> return user
  } 

module Suave =
  open Chessie
  open Chessie.ErrorHandling
  open Domain
  open Suave
  open Suave.DotLiquid
  open Suave.Filters
  open Suave.Form
  open Suave.Operators
  open Suave.Successful
  open User

  let loginTemplatePath = "user/login.liquid"

  type LoginViewModel = {
    Username: string
    Password: string
    Error: string option
  }

  let emptyLoginViewModel = {
    Username = ""
    Password = ""
    Error = None
  }

  let renderLoginPage (viewModel: LoginViewModel) =
    page loginTemplatePath viewModel

  let onLoginFailure viewModel loginError =
    let error =
      match loginError with
      | InvalidUsernameOrPassword ->
        "Invalid username or password"
      | EmailNotVerified ->
        "Email not verified"
      | Error ex ->
        printfn "[onLoginFailure] %A" ex
        "Something went wrong"
    let viewModel' = { viewModel with Error = Some error }
    renderLoginPage viewModel'

  let onLoginSuccess (user: User) =
    OK user.Username.Value

  let handleLoginResult viewModel result =
    Chessie.either onLoginSuccess (onLoginFailure viewModel) result  

  let handleLoginAsyncResult viewModel asyncResult =
    asyncResult
    |> Async.ofAsyncResult
    |> Async.map (handleLoginResult viewModel)

  let handleUserLogin findUser ctx = async {
    match bindEmptyForm ctx.request with
    | Choice1Of2 (viewModel: LoginViewModel) ->
      let result = LoginRequest.TryCreate (viewModel.Username, viewModel.Password)
      match result with
      | Success loginRequest ->
        let asyncResult = login findUser loginRequest
        let! webpart = handleLoginAsyncResult viewModel asyncResult
        return! webpart ctx
      | Failure error ->
        let viewModel' = { viewModel with Error = Some error }
        return! renderLoginPage viewModel' ctx
    | Choice2Of2 error ->
      let viewModel' = { emptyLoginViewModel with Error = Some error }
      return! renderLoginPage viewModel' ctx
  }  

  let webpart getDataContext =
    let findUser = Persistence.findUser getDataContext
    path "/login" >=>
      choose [
        GET >=> renderLoginPage emptyLoginViewModel
        POST >=> handleUserLogin findUser
      ]
