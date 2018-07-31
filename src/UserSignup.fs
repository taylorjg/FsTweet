namespace UserSignup

open System
module Suave =

  open Suave
  open Suave.Filters
  open Suave.Operators
  open Suave.DotLiquid

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
    printfn "%A" ctx.request.form
    return! Redirection.FOUND "/signup" ctx
  }

  let webPart =
    path "/signup" >=>
      choose [
        GET >=> page "user/signup.liquid" emptyUserSignupViewModel
        POST >=> handleUserSignup
      ]
