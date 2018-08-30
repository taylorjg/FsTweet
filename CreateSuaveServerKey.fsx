#r "/Users/jontaylor/.nuget/packages/suave/2.4.3/lib/netstandard2.0/Suave.dll"

open Suave.Utils
open System

Crypto.generateKey Crypto.KeyLength
|> Convert.ToBase64String
|> printfn "%s"
