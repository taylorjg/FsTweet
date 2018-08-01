namespace UserSignup

module Domain =

  open Chessie.ErrorHandling

  type Username = private Username of string with
    static member TryCreate (username: string) =
      match username with
      | null | "" -> fail "Username should not be empty"
      | x when x.Length > 12 -> fail "Username should not be more than 12 characters"
      | x -> Username x |> ok
    member this.Value =
      let (Username username) = this
      username

  type Password = private Password of string with
    static member TryCreate (password: string) =
      match password with
      | null | "" -> fail "Password should not be empty"
      | x when x.Length < 4 || x.Length > 8 -> fail "Password should contain only 4-8 characters"
      | x -> Password x |> ok
    member this.Value =
      let (Password password) = this
      password

  type EmailAddress = private EmailAddress of string with
    static member TryCreate (emailAddress: string) =
      try
        new System.Net.Mail.MailAddress(emailAddress) |> ignore
        EmailAddress emailAddress |> ok
      with
        | _ -> fail "Invalid email adddress"
    member this.Value =
      let (EmailAddress emailAddress) = this
      emailAddress

  type SignupUserRequest = {
    Username: Username
    Password: Password
    EmailAddress: EmailAddress
  } with
    static member TryCreate (username, password, emailAddress) =
      trial {
        let! username = Username.TryCreate username
        let! password = Password.TryCreate password
        let! emailAddress = EmailAddress.TryCreate emailAddress
        return {
          Username = username
          Password = password
          EmailAddress = emailAddress
        }
      }

module Suave =

  open Chessie.ErrorHandling
  open Domain
  open Suave
  open Suave.DotLiquid
  open Suave.Filters
  open Suave.Form
  open Suave.Operators

  let signupTemplatePath = "user/signup.liquid"

  type UserSignupViewModel = {
    Username: string
    Email: string
    Password: string
    Error: string option
  }

  let emptyUserSignupViewModel = {
    Username = ""
    Email = ""
    Password = ""
    Error = None
  }

  let handleUserSignup ctx = async {
    match bindEmptyForm ctx.request with
    | Choice1Of2 (vm: UserSignupViewModel) ->
      let result = SignupUserRequest.TryCreate (vm.Username, vm.Password, vm.Email)
      let onSuccess (signupUserRequest, _) =
        printfn "%A" signupUserRequest
        Redirection.FOUND "/signup" ctx
      let onFailure msgs =
        let msg = List.head msgs
        let viewModel = { vm with Error = Some msg }
        page signupTemplatePath viewModel ctx
      return! either onSuccess onFailure result      
    | Choice2Of2 msg ->
      let viewModel = { emptyUserSignupViewModel with Error = Some msg }
      return! page signupTemplatePath viewModel ctx
  }

  let webPart =
    path "/signup" >=>
      choose [
        GET >=> page signupTemplatePath emptyUserSignupViewModel
        POST >=> handleUserSignup
      ]
