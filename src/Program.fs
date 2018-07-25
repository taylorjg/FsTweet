open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful

let app = GET >=> path "/hello" >=> OK "Hello GET"

let config = { defaultConfig with bindings = [ HttpBinding.createSimple HTTP "0.0.0.0" 5000 ] }

[<EntryPoint>]
let main argv =
    startWebServer config app
    0
