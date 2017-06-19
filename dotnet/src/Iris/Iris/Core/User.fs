namespace rec Iris.Core

// * Imports

#if FABLE_COMPILER

open Path
open System
open Fable.Core
open Fable.Import
open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else
open Path
open System
open System.IO
open FlatBuffers
open Iris.Serialization

#endif

// * UserYaml

#if !FABLE_COMPILER && !IRIS_NODES

open SharpYaml.Serialization
open LibGit2Sharp

// __   __              _
// \ \ / /_ _ _ __ ___ | |
//  \ V / _` | '_ ` _ \| |
//   | | (_| | | | | | | |
//   |_|\__,_|_| |_| |_|_|

type UserYaml() =
  [<DefaultValue>] val mutable Id: string
  [<DefaultValue>] val mutable UserName: string
  [<DefaultValue>] val mutable FirstName: string
  [<DefaultValue>] val mutable LastName: string
  [<DefaultValue>] val mutable Email: string
  [<DefaultValue>] val mutable Password: string
  [<DefaultValue>] val mutable Salt: string
  [<DefaultValue>] val mutable Joined: DateTime
  [<DefaultValue>] val mutable Created: DateTime

  static member From(user: User) =
    let yaml = UserYaml()
    yaml.Id <- string user.Id
    yaml.UserName <- unwrap user.UserName
    yaml.FirstName <- unwrap user.FirstName
    yaml.LastName <- unwrap user.LastName
    yaml.Email <- unwrap user.Email
    yaml.Password <- unwrap user.Password
    yaml.Salt <- unwrap user.Salt
    yaml.Joined <- user.Joined
    yaml.Created <- user.Created
    yaml

  member yaml.ToUser() =
    Either.succeed { Id        = Id yaml.Id
                     UserName  = name yaml.UserName
                     FirstName = name yaml.FirstName
                     LastName  = name yaml.LastName
                     Email     = email yaml.Email
                     Password  = checksum yaml.Password
                     Salt      = checksum yaml.Salt
                     Joined    = yaml.Joined
                     Created   = yaml.Created }

#endif

// * User

//  _   _
// | | | |___  ___ _ __
// | | | / __|/ _ \ '__|
// | |_| \__ \  __/ |
//  \___/|___/\___|_|

[<CustomEquality;CustomComparison>]
type User =
  { Id:        Id
    UserName:  Name
    FirstName: Name
    LastName:  Name
    Email:     Email
    Password:  Hash
    Salt:      Hash
    Joined:    DateTime
    Created:   DateTime }

  // ** GetHashCode

  override me.GetHashCode() =
    let mutable hash = 42
    #if FABLE_COMPILER
    hash <- (hash * 7) + hashCode (string me.Id)
    hash <- (hash * 7) + (me.UserName  |> unwrap |> hashCode)
    hash <- (hash * 7) + (me.FirstName |> unwrap |> hashCode)
    hash <- (hash * 7) + (me.LastName  |> unwrap |> hashCode)
    hash <- (hash * 7) + (me.Email     |> unwrap |> hashCode)
    hash <- (hash * 7) + (me.Password  |> unwrap |> hashCode)
    hash <- (hash * 7) + (me.Salt      |> unwrap |> hashCode)
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

  // ** Equals

  override self.Equals(other) =
    match other with
    | :? User as user ->
      (self :> System.IEquatable<User>).Equals user
    | _ -> false

  // ** IEquatable.Equals

  interface System.IEquatable<User> with
    member me.Equals(other: User) =
      me.Id               = other.Id              &&
      me.UserName         = other.UserName        &&
      me.FirstName        = other.FirstName       &&
      me.LastName         = other.LastName        &&
      me.Email            = other.Email           &&
      me.Joined           = other.Joined          &&
      me.Created          = other.Created

  // ** IComparable.CompareTo

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

  // ** Signature

  #if !FABLE_COMPILER && !IRIS_NODES

  member user.Signature
    with get () =
      let name = String.Format("{0} {1}", user.FirstName, user.LastName)
      Signature(name, unwrap user.Email, DateTimeOffset(user.Created))

  // ** AssetPath

  member user.AssetPath
    with get () =
      USER_DIR <.> sprintf "%s%s" (string user.Id) ASSET_EXTENSION

  // ** Admin

  static member Admin
    with get () =
      { Id        = Id "cb558968-bd42-4de0-a671-18e2ec7cf580"
        UserName  = name Constants.ADMIN_USER_NAME
        FirstName = name Constants.ADMIN_FIRST_NAME
        LastName  = name Constants.ADMIN_LAST_NAME
        Email     = email Constants.ADMIN_EMAIL
        Password  = checksum ADMIN_DEFAULT_PASSWORD_HASH
        Salt      = checksum ADMIN_DEFAULT_SALT
        Joined    = DateTime.UtcNow
        Created   = DateTime.UtcNow }

  #endif

  // ** ToOffset

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id        = self.Id        |> string |> builder.CreateString
    let username  = self.UserName  |> unwrap |> Option.mapNull builder.CreateString
    let firstname = self.FirstName |> unwrap |> Option.mapNull builder.CreateString
    let lastname  = self.LastName  |> unwrap |> Option.mapNull builder.CreateString
    let email     = self.Email     |> unwrap |> Option.mapNull builder.CreateString
    let password  = self.Password  |> unwrap |> Option.mapNull builder.CreateString
    let salt      = self.Salt      |> unwrap |> Option.mapNull builder.CreateString
    let joined    = self.Joined.ToString("o")  |> builder.CreateString
    let created   = self.Created.ToString("o") |> builder.CreateString
    UserFB.StartUserFB(builder)
    UserFB.AddId(builder, id)
    Option.iter (fun value -> UserFB.AddUserName(builder, value)) username
    Option.iter (fun value -> UserFB.AddFirstName(builder, value)) firstname
    Option.iter (fun value -> UserFB.AddLastName(builder, value)) lastname
    Option.iter (fun value -> UserFB.AddEmail(builder, value)) email
    Option.iter (fun value -> UserFB.AddPassword(builder, value)) password
    Option.iter (fun value -> UserFB.AddSalt(builder, value)) salt
    UserFB.AddJoined(builder, joined)
    UserFB.AddCreated(builder, created)
    UserFB.EndUserFB(builder)

  // ** ToBytes

  member self.ToBytes() = Binary.buildBuffer self

  // ** FromFB

  static member FromFB(fb: UserFB) : Either<IrisError, User> =
    Either.tryWith (Error.asParseError "User.FromFB") <| fun _ ->
      { Id        = Id fb.Id
        UserName  = name     fb.UserName
        FirstName = name     fb.FirstName
        LastName  = name     fb.LastName
        Email     = email    fb.Email
        Password  = checksum fb.Password
        Salt      = checksum fb.Salt
        Joined    = DateTime.Parse fb.Joined
        Created   = DateTime.Parse fb.Created }

  // ** FromBytes

  static member FromBytes (bytes: byte[]) : Either<IrisError, User> =
    UserFB.GetRootAsUserFB(Binary.createBuffer bytes)
    |> User.FromFB

  // ** ToYamlObject

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  #if !FABLE_COMPILER && !IRIS_NODES

  member user.ToYamlObject () = UserYaml.From(user)

  // ** ToYaml

  member user.ToYaml(serializer: Serializer) =
    user |> Yaml.toYaml |> serializer.Serialize

  // ** FromYamlObject

  static member FromYamlObject (yaml: UserYaml) = yaml.ToUser()

  // ** FromYaml

  static member FromYaml(str: string) =
    let serializer = Serializer()
    serializer.Deserialize<UserYaml>(str)
    |> User.FromYamlObject

  #endif

  // ** Load

  //  _                    _
  // | |    ___   __ _  __| |
  // | |   / _ \ / _` |/ _` |
  // | |__| (_) | (_| | (_| |
  // |_____\___/ \__,_|\__,_|

  #if !FABLE_COMPILER && !IRIS_NODES

  static member Load(path: FilePath) : Either<IrisError, User> =
    IrisData.load path

  // ** LoadAll

  static member LoadAll(basePath: FilePath) : Either<IrisError, User array> =
    basePath </> filepath USER_DIR
    |> IrisData.loadAll

  // ** Save

  //  ____
  // / ___|  __ ___   _____
  // \___ \ / _` \ \ / / _ \
  //  ___) | (_| |\ V /  __/
  // |____/ \__,_| \_/ \___|

  member user.Save (basePath: FilePath) =
    IrisData.save basePath user

  #endif

// * User module

#if !FABLE_COMPILER

module User =

  // ** passwordValid

  let passwordValid (user: User) (password: Password) =
    let password = Crypto.hashPassword password user.Salt
    password = user.Password

#endif
