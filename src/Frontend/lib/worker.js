importScripts(
    "flatbuffers.js",
    "../js/Core_generated.js",
    "../js/Api_generated.js",
    "../js/Raft_generated.js",
    "../js/disco.worker.js"
);

onconnect = function (ev) {
    var port = ev.ports[0];
    var context = new DiscoWorker.WorkerContext();
    context.Register(port);
}
