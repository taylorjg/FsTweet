namespace Auth

module Domain =
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
open Domain

module Suave =
  open Chessie
  open Suave
  open Suave.DotLiquid
  open Suave.Filters
  open Suave.Form
  open Suave.Operators

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

  let handleUserLogin ctx = async {
    match bindEmptyForm ctx.request with
    | Choice1Of2 (viewModel: LoginViewModel) ->
      let result = LoginRequest.TryCreate (viewModel.Username, viewModel.Password)
      match result with
      | Success loginRequest ->
        let viewModel' = { viewModel with Error = Some "TODO" }
        return! renderLoginPage viewModel' ctx
      | Failure error ->
        let viewModel' = { viewModel with Error = Some error }
        return! renderLoginPage viewModel' ctx
    | Choice2Of2 error ->
      let viewModel' = { emptyLoginViewModel with Error = Some error }
      return! renderLoginPage viewModel' ctx
  }  

  let webpart () =
    path "/login" >=>
      choose [
        GET >=> renderLoginPage emptyLoginViewModel
        POST >=> handleUserLogin
      ]
