module User

open BCrypt.Net
open Chessie
open Chessie.ErrorHandling
open System.Security.Cryptography

let private base64URLEncoding bytes =
  let base64String = System.Convert.ToBase64String bytes
  base64String
    .TrimEnd([|'='|])
    .Replace('+', '-')
    .Replace('/', '_')

type UserId = UserId of int

type Username = private Username of string with
  static member TryCreate (username: string) =
    match username with
    | null | "" -> fail "Username should not be empty"
    | x when x.Length > 12 -> fail "Username should not be more than 12 characters"
    | x -> x.Trim().ToLowerInvariant() |> Username |> ok
  static member TryCreateAsync username =
    Username.TryCreate username
    |> mapFirstFailure System.Exception
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
