namespace UserSignup

module Domain =
  open BCrypt.Net
  open Chessie
  open Chessie.ErrorHandling
  open System.Security.Cryptography

  let base64URLEncoding bytes =
    let base64String = System.Convert.ToBase64String bytes
    base64String
      .TrimEnd([|'='|])
      .Replace('+', '-')
      .Replace('/', '_')

  type Username = private Username of string with
    static member TryCreate (username: string) =
      match username with
      | null | "" -> fail "Username should not be empty"
      | x when x.Length > 12 -> fail "Username should not be more than 12 characters"
      | x -> x.Trim().ToLowerInvariant() |> Username |> ok
    static member TryCreateAsync username =
      Username.TryCreate username
      |> mapFailureFirstItem System.Exception
      |> Async.singleton
      |> AR
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

  type PasswordHash = private PasswordHash of string with
    static member Create (password: Password) =
      BCrypt.HashPassword(password.Value) |> PasswordHash
    member this.Value =
      let (PasswordHash passwordHash) = this
      passwordHash

  type VerificationCode = private VerificationCode of string with
    static member Create () =
      let verificationCodeLength = 15
      let bytes:  byte [] = Array.zeroCreate verificationCodeLength
      use rng = new RNGCryptoServiceProvider()
      rng.GetBytes(bytes)
      base64URLEncoding(bytes) |> VerificationCode
    member this.Value =
      let (VerificationCode verificationCode) = this
      verificationCode

  type EmailAddress = private EmailAddress of string with
    static member TryCreate (emailAddress: string) =
      try
        new System.Net.Mail.MailAddress(emailAddress) |> ignore
        emailAddress.Trim().ToLowerInvariant() |> EmailAddress |> ok
      with
        | _ -> fail "Invalid email adddress"
    member this.Value =
      let (EmailAddress emailAddress) = this
      emailAddress

  type UserSignupRequest = {
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

  type CreateUserRequest = {
    Username: Username
    PasswordHash: PasswordHash
    EmailAddress: EmailAddress
    VerificationCode: VerificationCode
  }

  type UserId = UserId of int

  type SendSignupEmailRequest = {
    Username: Username
    EmailAddress: EmailAddress
    VerificationCode: VerificationCode
  }

  type CreateUserError =
  | EmailAlreadyExists
  | UsernameAlreadyExists
  | Error of System.Exception

  type CreateUser = CreateUserRequest -> AsyncResult<UserId, CreateUserError>

  type SendEmailError =
    SendEmailError of System.Exception

  type SendSignupEmail = SendSignupEmailRequest -> AsyncResult<unit, SendEmailError>

  type UserSignupError =
  | CreateUserError of CreateUserError
  | SendSignupEmailError of SendEmailError

  type UserSignup =
    CreateUser ->
      SendSignupEmail ->
      UserSignupRequest ->
      AsyncResult<UserId, UserSignupError>

  let userSignup
    (createUser: CreateUser)
    (sendSignupEmail: SendSignupEmail)
    (userSignupRequest: UserSignupRequest) = asyncTrial {
      let verificationCode = VerificationCode.Create()
      let createUserRequest = {
        Username = userSignupRequest.Username
        PasswordHash = PasswordHash.Create userSignupRequest.Password
        EmailAddress = userSignupRequest.EmailAddress
        VerificationCode = verificationCode
      }
      let! userId = createUser createUserRequest |> mapAsyncFailure CreateUserError
      let sendSignupEmailRequest = {
        Username = userSignupRequest.Username
        EmailAddress = userSignupRequest.EmailAddress
        VerificationCode = verificationCode
      }
      do! sendSignupEmail sendSignupEmailRequest |> mapAsyncFailure SendSignupEmailError
      return userId
    }

  type VerifyUser = string -> AsyncResult<Username option, System.Exception>  

module Persistence =
  open Chessie
  open Chessie.ErrorHandling
  open Database
  open Domain
  open Microsoft.EntityFrameworkCore
  open Npgsql

  let private (|UniqueViolation|_|) constraintName (ex: System.Exception) =
    match ex with
    | :? System.AggregateException as agEx ->
      // EF Core seems to wrap the PostgresException inside a DbUpdateException
      let ie = agEx.Flatten().InnerException
      let iie = ie.InnerException
      let innerException = if iie <> null then iie else ie
      match innerException with
      | :? PostgresException as pgEx ->
        match pgEx.ConstraintName = constraintName && pgEx.SqlState = "23505" with
        | true -> Some()
        | _ -> None
      | _ -> None
    | _ -> None

  let private mapException = function
    | UniqueViolation "IX_Users_Username" _ -> UsernameAlreadyExists
    | UniqueViolation "IX_Users_Email" _ -> EmailAlreadyExists
    | ex -> Error ex
  
  let createUser (getDataContext: GetDataContext) (createUserRequest: CreateUserRequest) = asyncTrial {
    let dbContext = getDataContext ()
    let newUser: User = {
      Id = 0
      Username = createUserRequest.Username.Value
      PasswordHash = createUserRequest.PasswordHash.Value
      Email = createUserRequest.EmailAddress.Value
      EmailVerificationCode = createUserRequest.VerificationCode.Value
      IsEmailVerified = false
    }
    dbContext.Users.Add(newUser) |> ignore
    do! saveChangesAsync dbContext |> mapAsyncFailure mapException
    printfn "[createUser] user created: %A" newUser
    return newUser.Id |> UserId
  }

  let toOption = function
    | Pass x -> Some x
    | _ -> None

  let verifyUser (getDataContext: GetDataContext) (verificationCode: string) = asyncTrial {
    let dbContext = getDataContext ()
    let queryable =
      query {
        for user in dbContext.Users do
          where (user.EmailVerificationCode = verificationCode)
          select user
      }
    let! userToVerify =
      EntityFrameworkQueryableExtensions.ToListAsync(queryable)
      |> Async.AwaitTask
      |> Async.Catch
      |> Async.map ofChoice
      |> AR
      |> mapAsyncSuccess (List.ofSeq >> List.tryHead)
    match userToVerify with
    | None -> return None
    | Some user ->
      let user' = { user with EmailVerificationCode = ""; IsEmailVerified = true }
      dbContext.Entry(user).CurrentValues.SetValues(user')
      do! saveChangesAsync dbContext
      let! username = Username.TryCreateAsync user.Username
      return Some username
  }

module Email =
  open Chessie
  open Chessie.ErrorHandling
  open Domain
  open Email

  let sendSignupEmail sendEmail sendSignupEmailRequest = asyncTrial {
    let placeHolders =
      Map.empty
        .Add("username", sendSignupEmailRequest.Username.Value)
        .Add("verificationCode", sendSignupEmailRequest.VerificationCode.Value)
    let templatedEmail = {
      To = sendSignupEmailRequest.EmailAddress.Value
      TemplateId = int64(7909690)
      PlaceHolders = placeHolders
    }      
    do! sendEmail templatedEmail |> mapAsyncFailure SendEmailError
    printfn "[sendSignupEmail] email sent: %A" sendSignupEmailRequest
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

  let signupTemplatePath = "user/signup.liquid"
  let signupSuccessTemplatePath = "user/signup_success.liquid"
  let verificationSuccessPath = "user/verification_success.liquid"
  let notFoundPath = "not_found.liquid"
  let serverErrorPath = "server_error.liquid"

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

  let onUserSignupSuccess viewModel _ =
    sprintf "/signup/success/%s" viewModel.Username |> Redirection.FOUND

  let handleCreateUserError viewModel = function
  | EmailAlreadyExists ->
    let viewModel = { viewModel with Error = Some "EmailAddress already exists" }
    page signupTemplatePath viewModel
  | UsernameAlreadyExists ->
    let viewModel = { viewModel with Error = Some "Username already exists" }
    page signupTemplatePath viewModel
  | Error ex ->
    printfn "[handleCreateUserError] server error: %A" ex
    let viewModel = { viewModel with Error = Some "Something went wrong" }
    page signupTemplatePath viewModel

  let handleSendSignupEmailError viewModel err =
    printfn "[handleSendSignupEmailError] error while sending signup email: %A" err
    let msg = "Something went wrong"
    let viewModel = { viewModel with Error = Some msg }
    page signupTemplatePath viewModel

  let onUserSignupError viewModel err =
    match err with
    | CreateUserError err -> handleCreateUserError viewModel err
    | SendSignupEmailError err -> handleSendSignupEmailError viewModel err

  let handleUserSignupResult viewModel result =
    Chessie.either
      (onUserSignupSuccess viewModel)
      (onUserSignupError viewModel)
      result
      
  let handleUserSignupAsyncResult viewModel asyncResult =
    asyncResult
    |> Async.ofAsyncResult
    |> Async.map (handleUserSignupResult viewModel)

  let handleUserSignup userSignup ctx = async {
    match bindEmptyForm ctx.request with
    | Choice1Of2 (viewModel: UserSignupViewModel) ->
      let result =
        UserSignupRequest.TryCreate(
          viewModel.Username,
          viewModel.Password,
          viewModel.Email)
      match result with
      | Ok (userSignupRequest, _) ->
        let asyncResult = userSignup userSignupRequest
        let! webpart = handleUserSignupAsyncResult viewModel asyncResult
        return! webpart ctx
      | Bad errors ->
        let error = List.head errors
        let viewModel' = { viewModel with Error = Some error }
        return! page signupTemplatePath viewModel' ctx
    | Choice2Of2 error ->
      let viewModel' = { emptyUserSignupViewModel with Error = Some error }
      return! page signupTemplatePath viewModel' ctx
  }

  let onVerificationSuccess verificationCode username =
    match username with
    | Some (username: Username) ->
      page verificationSuccessPath username.Value
    | _ ->
      printfn "[onVerificationSuccess] invalid verification code: %s" verificationCode
      page notFoundPath "Invalid verification code"

  let onVerificationFailure verificationCode (ex: System.Exception) =
    printfn
      "[onVerificationFailure] error while verifying email for verification code %s: %A"
      verificationCode
      ex
    page serverErrorPath "Error while verifying email"

  let handleVerifyUserAsyncResult verificationCode asyncResult =
    asyncResult
    |> Async.ofAsyncResult
    |> Async.map
      (Chessie.either
        (onVerificationSuccess verificationCode)
        (onVerificationFailure verificationCode))

  let handleSignupVerify (verifyUser: VerifyUser) verificationCode ctx = async {
    let asyncResult = verifyUser verificationCode
    let! webpart = handleVerifyUserAsyncResult verificationCode asyncResult
    return! webpart ctx
  }

  let webPart getDataContext sendEmail =
    let createUser = Persistence.createUser getDataContext
    let sendSignupEmail = Email.sendSignupEmail sendEmail
    let userSignup = Domain.userSignup createUser sendSignupEmail
    let verifyUser = Persistence.verifyUser getDataContext
    choose [
      path "/signup" >=>
        choose [
          GET >=> page signupTemplatePath emptyUserSignupViewModel
          POST >=> handleUserSignup userSignup
        ]
      pathScan "/signup/success/%s" (page signupSuccessTemplatePath)      
      pathScan "/signup/verify/%s" (handleSignupVerify verifyUser)
    ]
