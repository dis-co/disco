namespace Iris.Core

#if JAVASCRIPT

open Fable.Core
open Fable.Import
open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes
open Iris.Web.Core.UserFlatBuffers
#else

open System
open LibGit2Sharp
open FlatBuffers
open Iris.Serialization.Raft
open SharpYaml.Serialization

// __   __              _
// \ \ / /_ _ _ __ ___ | |
//  \ V / _` | '_ ` _ \| |
//   | | (_| | | | | | | |
//   |_|\__,_|_| |_| |_|_|

type UserYaml(i, u, f, l, e, j, c) =
  let mutable id        = i
  let mutable username  = u
  let mutable firstname = f
  let mutable lastname  = l
  let mutable email     = e
  let mutable joined    = j
  let mutable created   = c

  new () = new UserYaml(null, null, null, null, null, null, null)

  member self.Id
    with get ()  = id
     and set str = id <- str

  member self.UserName
    with get ()  = username
     and set str = username <- str

  member self.FirstName
    with get ()  = firstname
     and set str = firstname <- str

  member self.LastName
    with get ()  = lastname
     and set str = lastname <- str

  member self.Email
    with get ()  = email
     and set str = email <- str

  member self.Joined
    with get ()  = joined
     and set str = joined <- str

  member self.Created
    with get ()  = created
     and set str = created <- str

#endif

//  _   _
// | | | |___  ___ _ __
// | | | / __|/ _ \ '__|
// | |_| \__ \  __/ |
//  \___/|___/\___|_|

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

  override self.Equals(other) =
    match other with
    | :? User as user ->
      (self :> System.IEquatable<User>).Equals user
    | _ -> false

  interface System.IEquatable<User> with
    member me.Equals(other: User) =
      me.Id               = other.Id              &&
      me.UserName         = other.UserName        &&
      me.FirstName        = other.FirstName       &&
      me.LastName         = other.LastName        &&
      me.Email            = other.Email           &&
      (string me.Joined)  = (string other.Joined) &&
      (string me.Created) = (string other.Created)

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

  static member Admin
    with get () =
      { Id        = Id.Create()
      ; UserName  = "admin"
      ; FirstName = "Administrator"
      ; LastName  = ""
      ; Email     = "info@nsynk.de"
      ; Joined    = DateTime.Now
      ; Created   = DateTime.Now }

#endif

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

  static member FromFB(fb: UserFB) : Either<IrisError, User> =
    Either.tryWith ParseError "User" <| fun _ ->
      { Id        = Id fb.Id
      ; UserName  = fb.UserName
      ; FirstName = fb.FirstName
      ; LastName  = fb.LastName
      ; Email     = fb.Email
#if JAVASCRIPT
      ; Joined    = fb.Joined
      ; Created   = fb.Created }
#else
      ; Joined    = DateTime.Parse fb.Joined
      ; Created   = DateTime.Parse fb.Created }
#endif

  static member FromBytes (bytes: Binary.Buffer) : Either<IrisError, User> =
    UserFB.GetRootAsUserFB(Binary.createBuffer bytes)
    |> User.FromFB

#if JAVASCRIPT
#else

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  member self.ToYamlObject () =
    new UserYaml(
      string self.Id,
      self.UserName,
      self.FirstName,
      self.LastName,
      self.Email,
      string self.Joined,
      string self.Created)

  member self.ToYaml(serializer: Serializer) =
    self |> Yaml.toYaml |> serializer.Serialize

  static member FromYamlObject (yaml: UserYaml) =
    Either.tryWith ParseError "User" <| fun _ ->
      { Id = Id yaml.Id
        UserName = yaml.UserName
        FirstName = yaml.FirstName
        LastName = yaml.LastName
        Email = yaml.Email
        Joined = DateTime.Parse yaml.Joined
        Created = DateTime.Parse yaml.Created }

  static member FromYaml(str: string) =
    let serializer = new Serializer()
    serializer.Deserialize<UserYaml>(str)
    |> User.FromYamlObject

  member self.DirName
    with get () = "users"

  member self.CanonicalName
    with get () =
      sprintf "%s_%s" self.FirstName self.LastName
      |> String.toLower
      |> sanitizeName
      |> sprintf "%s-%s" (string self.Id)
#endif
