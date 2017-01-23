namespace Iris.Core

open System

[<AutoOpen>]
module Constants =

  [<Literal>]
  let WEP_API_COMMAND = "api/command"

  //  ____        __ _
  // |  _ \ __ _ / _| |_
  // | |_) / _` | |_| __|
  // |  _ < (_| |  _| |_
  // |_| \_\__,_|_|  \__|

  [<Literal>]
  let RAFT_DIRECTORY = ".raft"

  [<Literal>]
  let RAFT_METADATA_FILENAME = "metadata"

  [<Literal>]
  let RAFT_LOGDATA_PATH = "logs"

  //  ___      _
  // |_ _|_ __(_)___
  //  | || '__| / __|
  //  | || |  | \__ \
  // |___|_|  |_|___/

  [<Literal>]
  let PROJECT_FILENAME = "project"

  [<Literal>]
  let ASSET_EXTENSION = ".yaml"

  [<Literal>]
  let ZEROCONF_TCP_SERVICE = "_iris._tcp"

  [<Literal>]
  let ZEROCONF_UDP_SERVICE = "_iris._udp"

  //  ____        __             _ _
  // |  _ \  ___ / _| __ _ _   _| | |_ ___
  // | | | |/ _ \ |_ / _` | | | | | __/ __|
  // | |_| |  __/  _| (_| | |_| | | |_\__ \
  // |____/ \___|_|  \__,_|\__,_|_|\__|___/

  [<Literal>]
  let EMPTY = "<empty>"

  [<Literal>]
  let REQUEST_TIMEOUT = 5000

  [<Literal>]
  let WEB_WORKER_SCRIPT = "lib/worker.js"

  [<Literal>]
  let DEFAULT_IP = "0.0.0.0"

  [<Literal>]
  let DEFAULT_WEB_PORT = 7000us

  [<Literal>]
  let ADMIN_DEFAULT_PASSWORD =
    // "Nsynk"
    "9305b34e6df2f0ee0a7aab083cb7c47761f4320cce0a0a6a35f6974c95483366"

  [<Literal>]
  let ADMIN_DEFAULT_SALT = "8d406594282be466e048de02505ebaec97943096"

  [<Literal>]
  let USER_DIR = "users"

  [<Literal>]
  let CUE_DIR = "cues"

  [<Literal>]
  let CUELIST_DIR = "cuelists"

  [<Literal>]
  let PATCH_DIR = "patches"

  //  __  __            _     _             ____             __ _
  // |  \/  | __ _  ___| |__ (_)_ __   ___ / ___|___  _ __  / _(_) __ _
  // | |\/| |/ _` |/ __| '_ \| | '_ \ / _ \ |   / _ \| '_ \| |_| |/ _` |
  // | |  | | (_| | (__| | | | | | | |  __/ |__| (_) | | | |  _| | (_| |
  // |_|  |_|\__,_|\___|_| |_|_|_| |_|\___|\____\___/|_| |_|_| |_|\__, |
  //                                                              |___/

  [<Literal>]
  let MACHINECONFIG_DEFAULT_PATH = "etc"

  [<Literal>]
  let MACHINECONFIG_NAME = "machinecfg"

  [<Literal>]
  let GITIGNORE = @"/.raft"

  [<Literal>]
  /// Intended to be a subdirectory of HOME
  let MACHINECONFIG_DEFAULT_WORKSPACE_UNIX = "iris"

  [<Literal>]
  let MACHINECONFIG_DEFAULT_WORKSPACE_WINDOWS = "C:\Iris"
