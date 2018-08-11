module Chessie

open Chessie.ErrorHandling

let mapFailureFirstItem f result =
  let mapFirstItem xs = List.head xs |> f |> List.singleton
  mapFailure mapFirstItem result

let mapAsyncFailure f asyncResult =
  asyncResult
    |> Async.ofAsyncResult
    |> Async.map (mapFailureFirstItem f)
    |> AR

let mapAsyncSuccess f asyncResult =
  asyncResult
    |> Async.ofAsyncResult
    |> Async.map (lift f)
    |> AR

let private onSuccessAdapter f (x, _) = f x

let private onFailureAdapter f = function
  | x :: _ -> f x
  | [] -> failwith "Chessie.onFailure called with an empty list"

let either onSuccess onFailure =
  either (onSuccessAdapter onSuccess) (onFailureAdapter onFailure)
