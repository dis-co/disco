window.IrisPlugins = window.IrisPlugins || [];

(function(plugins) {
  var h = virtualDom.h;

  function trigger(ev, listeners, ctx) {
    listeners.forEach(function(l) {
      l.call({ event: ev }, ctx);
    });
  }

  function sliceView(ctx) {
    console.log('value plugin', ctx);
    return function(slice) {
      return h('div', [
        h('strong', [slice.idx]),
        h('input', {
          type: 'number',
          onchange: function (ev) {
            trigger(ev, ctx.listeners, ctx);
          }
        }, [slice.value])
      ]);
    };
  }
  
  var ValuePlug = function() {
    this.listeners = [];

    // get current IOBox values
    this.get = function() {
      console.log("get called", arguments);
      return this.slices;
    };

    // register callback on update events to UI
    this.register = function(cb) {
      this.listeners.push(cb);
    };

    this.dispose = function() {
      console.log('disposing')
    };

    // render is expected to return a VTree even for static things like canvas
    this.render = function (iobox) {
      return h('div', iobox.slices.map(sliceView(this)));
    };
  };

  plugins.push({
    name: "Value Plugin",
    type: "value",
    create: function() {
      return new ValuePlug(arguments);
    }
  });
})(window.IrisPlugins);
