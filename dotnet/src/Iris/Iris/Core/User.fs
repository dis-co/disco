namespace Iris.Core

#if JAVASCRIPT

open Fable.Core
open Fable.Import

#else

open System
open LibGit2Sharp
open FlatBuffers
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Iris.Serialization.Raft

#endif

[<CustomEquality>]
[<CustomComparison>]
type User =
  { Id:        Id
  ; UserName:  Name
  ; FirstName: Name
  ; LastName:  Name
  ; Email:     Email
#if JAVASCRIPT
  ; Joined:    string
  ; Created:   string }
#else
  ; Joined:    DateTime
  ; Created:   DateTime }
#endif


  override me.GetHashCode() =
    let mutable hash = 42
#if JAVASCRIPT
    let mutable hash = 42
    hash <- (hash * 7) + hashCode (string me.Id)
    hash <- (hash * 7) + hashCode me.UserName
    hash <- (hash * 7) + hashCode me.FirstName
    hash <- (hash * 7) + hashCode me.LastName
    hash <- (hash * 7) + hashCode me.Email
    hash <- (hash * 7) + hashCode (string me.Joined)
    hash <- (hash * 7) + hashCode (string me.Created)
#else
    hash <- (hash * 7) + me.Id.GetHashCode()
    hash <- (hash * 7) + me.UserName.GetHashCode()
    hash <- (hash * 7) + me.FirstName.GetHashCode()
    hash <- (hash * 7) + me.LastName.GetHashCode()
    hash <- (hash * 7) + me.Email.GetHashCode()
    hash <- (hash * 7) + (string me.Joined).GetHashCode()
    hash <- (hash * 7) + (string me.Created).GetHashCode()
#endif
    hash

  override me.Equals(o) =
    match o with
    | :? User ->
      let other = o :?> User
      me.Id               = other.Id              &&
      me.UserName         = other.UserName        &&
      me.FirstName        = other.FirstName       &&
      me.LastName         = other.LastName        &&
      me.Email            = other.Email           &&
      (string me.Joined)  = (string other.Joined) &&
      (string me.Created) = (string other.Created)
    | _ -> false

  interface System.IComparable with
    member me.CompareTo(o: obj) =
      match o with
      | :? User ->
        let other = o :?> User

#if JAVASCRIPT
        if me.UserName > other.UserName then
          1
        elif me.UserName = other.UserName then
          0
        else
          -1
#else
        let arr = [| me.UserName; other.UserName |] |> Array.sort
        if Array.findIndex ((=) me.UserName) arr = 0 then
          -1
        else
          1
#endif

      | _ -> 0


#if JAVASCRIPT
#else

  member user.Signature
    with get () =
      let name = sprintf "%s %s" user.FirstName user.LastName
      new Signature(name, user.Email, new DateTimeOffset(user.Created))

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id        = self.Id        |> string |> builder.CreateString
    let username  = self.UserName  |> builder.CreateString
    let firstname = self.FirstName |> builder.CreateString
    let lastname  = self.LastName  |> builder.CreateString
    let email     = self.Email     |> builder.CreateString
    let joined    = self.Joined    |> string |> builder.CreateString
    let created   = self.Created   |> string |> builder.CreateString
    UserFB.StartUserFB(builder)
    UserFB.AddId(builder, id)
    UserFB.AddUserName(builder, username)
    UserFB.AddFirstName(builder, firstname)
    UserFB.AddLastName(builder, lastname)
    UserFB.AddEmail(builder, email)
    UserFB.AddJoined(builder, joined)
    UserFB.AddCreated(builder, created)
    UserFB.EndUserFB(builder)

  member self.ToBytes() = Binary.buildBuffer self

  static member FromFB(fb: UserFB) : User option =
    try
      { Id        = Id fb.Id
      ; UserName  = fb.UserName
      ; FirstName = fb.FirstName
      ; LastName  = fb.LastName
      ; Email     = fb.Email
      ; Joined    = DateTime.Parse fb.Joined
      ; Created   = DateTime.Parse fb.Created }
      |> Some
    with
      | exn ->
        printfn "Could not de-serializae binary rep of User: %s" exn.Message
        None

  static member FromBytes (bytes: byte array) : User option =
    UserFB.GetRootAsUserFB(new ByteBuffer(bytes))
    |> User.FromFB

  //      _
  //     | |___  ___  _ __
  //  _  | / __|/ _ \| '_ \
  // | |_| \__ \ (_) | | | |
  //  \___/|___/\___/|_| |_|

  member self.ToJToken() =
    new JObject()
    |> addString "Id"        (string self.Id)
    |> addString "UserName"  (string self.UserName)
    |> addString "FirstName"  self.FirstName
    |> addString "LastName"   self.LastName
    |> addString "Email"      self.Email
    |> addString "Joined"    (string self.Joined)
    |> addString "Created"   (string self.Created)

  member self.ToJson() =
    self.ToJToken() |> string

  static member FromJToken(token: JToken) : User option =
    try
      { Id        = Id (string token.["Id"])
      ; UserName  = (string token.["UserName"])
      ; FirstName = (string token.["FirstName"])
      ; LastName  = (string token.["LastName"])
      ; Email     = (string token.["Email"])
      ; Joined    = DateTime.Parse (string token.["Joined"])
      ; Created   = DateTime.Parse (string token.["Created"])
      } |> Some
    with
      | exn ->
        printfn "Could not deserialize user json: "
        printfn "    Message: %s"  exn.Message
        printfn "    json:    %s" (string token)
        None

  static member FromJson(str: string) : User option =
    JToken.Parse(str) |> User.FromJToken

#endif
