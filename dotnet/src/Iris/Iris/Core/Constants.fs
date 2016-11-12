namespace Iris.Core

[<AutoOpen>]
module Constants =

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

  //  _____             __     __
  // | ____|_ ____   __ \ \   / /_ _ _ __ ___
  // |  _| | '_ \ \ / /  \ \ / / _` | '__/ __|
  // | |___| | | \ V /    \ V / (_| | |  \__ \
  // |_____|_| |_|\_/      \_/ \__,_|_|  |___/

  [<Literal>]
  let IRIS_WORKSPACE = "IRIS_WORKSPACE"

  [<Literal>]
  let IRIS_VERBOSE = "IRIS_VERBOSE"

  [<Literal>]
  let IRIS_LOGGING_OFFSET = "IRIS_LOGGING_OFFSET"

  [<Literal>]
  let IRIS_NODE_ID = "IRIS_NODE_ID"

  [<Literal>]
  let WEB_WORKER_SCRIPT = "js/worker.js"

  [<Literal>]
  let SOCKET_SERVER_PORT_DIFF = 1000
