importScripts("flatbuffers.js", "../js/Core_generated.js", "../js/Api_generated.js", "../js/Raft_generated.js", "require.js");
onconnect = function (ev) {
    var port = ev.ports[0];
    require({
        paths: {
            'fable-core/umd': '../js/fable-core',
            'fable-powerpack/umd': '../js/fable-powerpack'
        }
    },
    ["../js/Web/Core/Worker"], function (worker) {
        var context = new worker.GlobalContext();
        context.Register(port);
    });
}
