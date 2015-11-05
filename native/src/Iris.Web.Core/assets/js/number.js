window.IrisPlugins = window.IrisPlugins || [];

(function(plugins) {
  var h = virtualDom.h;

  function trigger(ev, listeners, ctx) {
    listeners.forEach(function(l) {
      l.call({ event: ev }, ctx);
    });
  }

  function sliceView(slice) {
    return h('div', [
      h('h3', ['Slice: ' + slice.idx]),
      h('p', ['value: ' + slice.value])
    ]);
  }
  
  var myplugin = function () {
    console.log('myplugin instance constructor');

    this.listeners = {};

    // get current IOBox values
    this.get = function() {
      console.log("get called", arguments);
      return this.slices;
    };

    // register callback on update events to UI
    this.on = function(tag, cb) {
      console.log("on called", arguments);
      this.listeners[tag] = cb;
    };

    // unregister callback
    this.off = function(tag) {
      console.log("off called", arguments);
      this.listeners[tag] = null;
    };

    this.dispose = function() {
      console.log("dispose called");
    };

    // render is expected to return a VTree even for static things like canvas
    this.render = function (iobox) {
      return h('div', iobox.slices.map(sliceView));
    };
  };
  
  plugins.push({
    name: "test-plugin",
    type: "number",
    create: function() {
      console.log("create called");
      return new myplugin(arguments);
    }
  });
})(window.IrisPlugins);
