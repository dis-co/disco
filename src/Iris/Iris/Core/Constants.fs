namespace Iris.Core

open System

[<AutoOpen>]
module Constants =

  [<Literal>]
  let QUEUE_LENGTH_THRESHOLD = 100      // number messages in queus to trigger warning

  [<Literal>]
  let WEP_API_COMMAND = "/api/command"

  //  _____
  // |__  /_ __ ___   __ _
  //   / /| '_ ` _ \ / _` |
  //  / /_| | | | | | (_| |
  // /____|_| |_| |_|\__, |
  //                    |_|

  [<Literal>]
  let REQ_TIMEOUT = 500.0            // ms

  [<Literal>]
  let API_BACKEND_PREFIX = "apibackend"

  [<Literal>]
  let API_CLIENT_PREFIX = "apiclient"

  [<Literal>]
  let RAFT_BACKEND_PREFIX = "raftbackend"

  //  ____        __ _
  // |  _ \ __ _ / _| |_
  // | |_) / _` | |_| __|
  // |  _ < (_| |  _| |_
  // |_| \_\__,_|_|  \__|

  [<Literal>]
  let RAFT_DIRECTORY = ".raft"

  [<Literal>]
  let SNAPSHOT_FILENAME = "snapshot"

  [<Literal>]
  let RAFT_METADATA_FILENAME = "metadata"

  [<Literal>]
  let RAFT_LOGDATA_PATH = "logs"

  [<Literal>]
  let RAFT_ELECTION_TIMEOUT = 600       // see page 10 on timings in https://raft.github.io/raft.pdf

  [<Literal>]
  let RAFT_REQUEST_TIMEOUT = 100        // see ^

  [<Literal>]
  let RAFT_MAX_LOGDEPTH = 50

  [<Literal>]
  let RAFT_PERIODIC_INTERVAL = 400       // ms

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

  [<Literal>]
  let ZEROCONF_DOMAIN = "local."

  [<Literal>]
  let ZEROCONF_SERVICE_NAME = "Iris"

  //  ____        __             _ _
  // |  _ \  ___ / _| __ _ _   _| | |_ ___
  // | | | |/ _ \ |_ / _` | | | | | __/ __|
  // | |_| |  __/  _| (_| | |_| | | |_\__ \
  // |____/ \___|_|  \__,_|\__,_|_|\__|___/

  [<Literal>]
  let EMPTY = "<empty>"

  [<Literal>]
  let DEFAULT = "default"

  [<Literal>]
  let COMMAND_TIMEOUT = 5000

  [<Literal>]
  let WEB_WORKER_SCRIPT = "lib/worker.js"

  [<Literal>]
  let DEFAULT_IP = "0.0.0.0"

  [<Literal>]
  let DEFAULT_API_PORT = 5000us

  [<Literal>]
  let DEFAULT_API_CLIENT_PORT = 10000us

  [<Literal>]
  let DEFAULT_RAFT_PORT = 6000us

  [<Literal>]
  let DEFAULT_LOGFILE_NAME = "iris-log.txt"

  [<Literal>]
  let DEFAULT_HTTP_PORT = 7000us

  [<Literal>]
  let DEFAULT_WEB_SOCKET_PORT = 8000us

  [<Literal>]
  let DEFAULT_GIT_PORT = 9000us

  [<Literal>]
  let DEFAULT_MCAST_ADDRESS = "224.0.0.2"

  [<Literal>]
  let DEFAULT_MCAST_PORT = 5555us

  [<Literal>]
  let ADMIN_USER_NAME = "admin"

  [<Literal>]
  let ADMIN_EMAIL = "admin@nsynk.de"

  [<Literal>]
  let ADMIN_FIRST_NAME = "Administrator"

  [<Literal>]
  let ADMIN_LAST_NAME = ""

  [<Literal>]
  let ADMIN_DEFAULT_PASSWORD = "Nsynk"

  [<Literal>]
  let ADMIN_DEFAULT_PASSWORD_HASH =
    // "Nsynk"
    "9305b34e6df2f0ee0a7aab083cb7c47761f4320cce0a0a6a35f6974c95483366"

  [<Literal>]
  let ADMIN_DEFAULT_SALT = "8d406594282be466e048de02505ebaec97943096"

  [<Literal>]
  let DEFAULT_ASSET_FILTER = ".tga .png .jpg"

  //     _                 _
  //    / \   ___ ___  ___| |_ ___
  //   / _ \ / __/ __|/ _ \ __/ __|
  //  / ___ \\__ \__ \  __/ |_\__ \
  // /_/   \_\___/___/\___|\__|___/

  [<Literal>]
  let USER_DIR = "users"

  [<Literal>]
  let CUE_DIR = "cues"

  [<Literal>]
  let CUELIST_DIR = "cuelists"

  [<Literal>]
  let PINGROUP_DIR = "pingroups"

  [<Literal>]
  let PINMAPPING_DIR = "pinmappings"

  [<Literal>]
  let CUEPLAYER_DIR = "players"

  [<Literal>]
  let PINWIDGET_DIR = "widgets"

  [<Literal>]
  let CUEPLAYER_GROUP_ID = "0f0f825d-ce36-41f1-8143-de40f7cf1144"

  [<Literal>]
  let PINWIDGET_GROUP_ID = "86b15b97-6769-4462-a17c-8c2d1a5309b4"

  let GLOBAL_ASSET_DIRS =
    [ USER_DIR
      CUE_DIR
      CUELIST_DIR
      PINGROUP_DIR
      PINMAPPING_DIR
      PINWIDGET_DIR
      CUEPLAYER_DIR ]

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
  let GITIGNORE = @"
.raft/snapshot.yaml
.raft/metadata.yaml
"

  [<Literal>]
  /// Intended to be a subdirectory of $HOME
  let MACHINECONFIG_DEFAULT_WORKSPACE_UNIX = "iris"

  [<Literal>]
  let MACHINECONFIG_DEFAULT_WORKSPACE_WINDOWS = "C:\Iris"

  [<Literal>]
  /// Intended to be a subdirectory of $HOME/iris
  let MACHINECONFIG_DEFAULT_ASSET_DIRECTORY_UNIX = "assets"

  [<Literal>]
  let MACHINECONFIG_DEFAULT_ASSET_DIRECTORY_WINDOWS = "C:\Iris\assets"

  //  _____            _                                      _
  // | ____|_ ____   _(_)_ __ ___  _ __  _ __ ___   ___ _ __ | |_
  // |  _| | '_ \ \ / / | '__/ _ \| '_ \| '_ ` _ \ / _ \ '_ \| __|
  // | |___| | | \ V /| | | | (_) | | | | | | | | |  __/ | | | |_
  // |_____|_| |_|\_/ |_|_|  \___/|_| |_|_| |_| |_|\___|_| |_|\__|

  [<Literal>]
  let IRIS_CLIENT_ID_ENV_VAR = "IRIS_CLIENT_ID"

  module ErrorMessages =
    [<Literal>]
    let PROJECT_NO_ACTIVE_CONFIG = "No active cluster configuration"

    [<Literal>]
    let PROJECT_MISSING_CLUSTER = "Missing active cluster configuration"

    [<Literal>]
    let PROJECT_MISSING_MEMBER = "Missing member"

    [<Literal>]
    let PROJECT_MEMBER_MISMATCH = "is different from Machine"
