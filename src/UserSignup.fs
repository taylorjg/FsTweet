namespace UserSignup

open System
module Suave =

  open Suave
  open Suave.DotLiquid
  open Suave.Filters
  open Suave.Form
  open Suave.Operators

  type UserSignupViewModel = {
    Username: String
    Email: String
    Password: String
    Error: String option
  }

  let emptyUserSignupViewModel = {
    Username = ""
    Email = ""
    Password = ""
    Error = None
  }

  let handleUserSignup ctx = async {
    match bindEmptyForm ctx.request with
    | Choice1Of2 (userSignupViewModel: UserSignupViewModel) ->
      printfn "%A" userSignupViewModel
      return! Redirection.FOUND "/signup" ctx
    | Choice2Of2 err ->
      let viewModel = { emptyUserSignupViewModel with Error = Some err }
      return! page "user/signup.liquid" viewModel ctx
  }

  let webPart =
    path "/signup" >=>
      choose [
        GET >=> page "user/signup.liquid" emptyUserSignupViewModel
        POST >=> handleUserSignup
      ]
