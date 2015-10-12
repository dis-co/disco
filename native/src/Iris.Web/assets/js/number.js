window.IrisPlugins = window.IrisPlugins || [];

(function(plugins) {
  function trigger(ev, listeners, ctx) {
    listeners.forEach(function(l) {
      l.call({ event: ev }, ctx);
    });
  }
  
  var myplugin = function () {
    console.log('myplugin instance constructor');

    this.slices = [];
    this.listeners = {};

    // get current IOBox values
    this.get = function() {
      console.log("get called", arguments);
      return this.slices;
    };

    // set display to supplied values 
    this.set = function(values) {
      console.log("set called", arguments);
      this.slices = values;
    };

    // update entire IOBox (incl. metadata)
    this.update = function(iobox) {
      console.log("update called", arguments);
      this.slices = iobox.Slices;
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
      this.slices = [];
      this.listeners = {};
    };

    // render is expected to return a VTree even for static things like canvas
    this.render = function () {
      console.log("render called");
      this.tree = virtualDom.h('h1', ['hello']);
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
