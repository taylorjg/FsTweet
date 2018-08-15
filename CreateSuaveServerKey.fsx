#r "./packages/Suave/lib/net461/Suave.dll"

open Suave.Utils
open System

Crypto.generateKey Crypto.KeyLength
|> Convert.ToBase64String
|> printfn "%s"
