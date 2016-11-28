importScripts("flatbuffers.js", "Raft_generated.js", "../node_modules/requirejs/require.js");
onconnect = function (ev) {
    var port = ev.ports[0];
    require({
        paths: { 'fable-core/umd': '../node_modules/fable-core/umd' }
    },
    ["./Web/Core/Worker"], function (worker) {
        var context = new worker.GlobalContext();
        context.Register(port);
    });
}
