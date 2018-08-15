namespace Wall

module Suave =
  open Auth.Suave
  open Suave
  open Suave.Filters
  open Suave.Operators
  open User

  let private renderWall (user: User) ctx = async {
    return! Successful.OK user.Username.Value ctx
  }

  let webpart () =
    path "/wall" >=> requiresAuth renderWall
