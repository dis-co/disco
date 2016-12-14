namespace Iris.Core

#if FABLE_COMPILER

open Fable.Core
open Fable.Import
open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

open System
open System.IO
open LibGit2Sharp
open FlatBuffers
open Iris.Serialization.Raft
open SharpYaml.Serialization

// __   __              _
// \ \ / /_ _ _ __ ___ | |
//  \ V / _` | '_ ` _ \| |
//   | | (_| | | | | | | |
//   |_|\__,_|_| |_| |_|_|

type UserYaml(i, u, f, l, e, p, s, j, c) =
  let mutable id        = i
  let mutable username  = u
  let mutable firstname = f
  let mutable lastname  = l
  let mutable email     = e
  let mutable password  = p
  let mutable salt      = s
  let mutable joined    = j
  let mutable created   = c

  new () = new UserYaml(null, null, null, null, null, null, null, null, null)

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

  member self.Password
    with get ()  = password
     and set str = password <- str

  member self.Salt
    with get ()  = salt
     and set str = salt <- str

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

[<CustomEquality;CustomComparison>]
type User =
  { Id:        Id
  ; UserName:  Name
  ; FirstName: Name
  ; LastName:  Name
  ; Email:     Email
  ; Password:  string
  ; Salt:      string
  #if FABLE_COMPILER
  ; Joined:    string
  ; Created:   string }
  #else
  ; Joined:    DateTime
  ; Created:   DateTime }
  #endif

  override me.GetHashCode() =
    let mutable hash = 42
    #if FABLE_COMPILER
    hash <- (hash * 7) + hashCode (string me.Id)
    hash <- (hash * 7) + hashCode me.UserName
    hash <- (hash * 7) + hashCode me.FirstName
    hash <- (hash * 7) + hashCode me.LastName
    hash <- (hash * 7) + hashCode me.Email
    hash <- (hash * 7) + hashCode me.Password
    hash <- (hash * 7) + hashCode me.Salt
    hash <- (hash * 7) + hashCode (string me.Joined)
    hash <- (hash * 7) + hashCode (string me.Created)
    #else
    hash <- (hash * 7) + me.Id.GetHashCode()
    hash <- (hash * 7) + me.UserName.GetHashCode()
    hash <- (hash * 7) + me.FirstName.GetHashCode()
    hash <- (hash * 7) + me.LastName.GetHashCode()
    hash <- (hash * 7) + me.Email.GetHashCode()
    hash <- (hash * 7) + me.Password.GetHashCode()
    hash <- (hash * 7) + me.Salt.GetHashCode()
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

        #if FABLE_COMPILER
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


#if !FABLE_COMPILER

  member user.Signature
    with get () =
      let name = sprintf "%s %s" user.FirstName user.LastName
      new Signature(name, user.Email, new DateTimeOffset(user.Created))

  member user.AssetPath
    with get () =
      let filename =
        sprintf "%s_%s%s"
          (String.sanitize user.UserName)
          (string user.Id)
          ASSET_EXTENSION
      USER_DIR </> filename

  static member Admin
    with get () =
      { Id        = Id "cb558968-bd42-4de0-a671-18e2ec7cf580"
      ; UserName  = "admin"
      ; FirstName = "Administrator"
      ; LastName  = ""
      ; Email     = "admin@nsynk.de"
      ; Password  = ADMIN_DEFAULT_PASSWORD
      ; Salt      = ADMIN_DEFAULT_SALT
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
    let password  = self.Password  |> builder.CreateString
    let salt      = self.Salt      |> builder.CreateString
    let joined    = self.Joined    |> string |> builder.CreateString
    let created   = self.Created   |> string |> builder.CreateString
    UserFB.StartUserFB(builder)
    UserFB.AddId(builder, id)
    UserFB.AddUserName(builder, username)
    UserFB.AddFirstName(builder, firstname)
    UserFB.AddLastName(builder, lastname)
    UserFB.AddEmail(builder, email)
    UserFB.AddPassword(builder, password)
    UserFB.AddSalt(builder, salt)
    UserFB.AddJoined(builder, joined)
    UserFB.AddCreated(builder, created)
    UserFB.EndUserFB(builder)

  member self.ToBytes() = Binary.buildBuffer self

  static member FromFB(fb: UserFB) : Either<IrisError, User> =
    Either.tryWith (Error.asParseError "User.FromFB") <| fun _ ->
      { Id        = Id fb.Id
        UserName  = fb.UserName
        FirstName = fb.FirstName
        LastName  = fb.LastName
        Email     = fb.Email
        Password  = fb.Password
        Salt      = fb.Salt
        #if FABLE_COMPILER
        Joined    = fb.Joined
        Created   = fb.Created }
        #else
        Joined    = DateTime.Parse fb.Joined
        Created   = DateTime.Parse fb.Created }
        #endif

  static member FromBytes (bytes: Binary.Buffer) : Either<IrisError, User> =
    UserFB.GetRootAsUserFB(Binary.createBuffer bytes)
    |> User.FromFB

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  #if !FABLE_COMPILER

  member self.ToYamlObject () =
    new UserYaml(
      string self.Id,
      self.UserName,
      self.FirstName,
      self.LastName,
      self.Email,
      self.Password,
      self.Salt,
      string self.Joined,
      string self.Created)

  member self.ToYaml(serializer: Serializer) =
    self |> Yaml.toYaml |> serializer.Serialize

  static member FromYamlObject (yaml: UserYaml) =
    Either.tryWith (Error.asParseError "User.FromYaml") <| fun _ ->
      { Id        = Id yaml.Id
        UserName  = yaml.UserName
        FirstName = yaml.FirstName
        LastName  = yaml.LastName
        Email     = yaml.Email
        Password  = yaml.Password
        Salt      = yaml.Salt
        Joined    = DateTime.Parse yaml.Joined
        Created   = DateTime.Parse yaml.Created }

  static member FromYaml(str: string) =
    let serializer = new Serializer()
    serializer.Deserialize<UserYaml>(str)
    |> User.FromYamlObject

  #endif

  //  _                    _
  // | |    ___   __ _  __| |
  // | |   / _ \ / _` |/ _` |
  // | |__| (_) | (_| | (_| |
  // |_____\___/ \__,_|\__,_|

  #if !FABLE_COMPILER

  static member Load(path: FilePath) : Either<IrisError, User> =
    either {
      let! data = Asset.read path
      let! user = Yaml.decode data
      return user
    }

  static member LoadAll(basePath: FilePath) : Either<IrisError, User array> =
    either {
      try
        let dir = basePath </> USER_DIR
        let files = Directory.GetFiles(dir, sprintf "*%s" ASSET_EXTENSION)

        let! (_,users) =
          let arr =
            files
            |> Array.length
            |> Array.zeroCreate

          Array.fold
            (fun (m: Either<IrisError, int * User array>) path ->
              either {
                let! (idx,users) = m
                let! user = User.Load path
                users.[idx] <- user
                return (idx + 1, users)
              })
            (Right(0, arr))
            files

        return users
      with
        | exn ->
          return!
            exn.Message
            |> Error.asAssetError "User.LoadAll"
            |> Either.fail
    }

  #endif
