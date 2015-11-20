window.IrisPlugins = window.IrisPlugins || [];

(function(plugins) {

  var ValuePlug = function() {
    var listeners = [];
    var h = virtualDom.h;

    function trigger(ev) {
      listeners.forEach(function(listener) {
        listener.call({ slices: ev });
      });
    }

    function sliceView(ctx) {
      return function(slice) {
        return h('div', [
          h('strong', [slice.idx]),
          h('input', {
            type: 'number',
            onchange: function (ev) {
              trigger({ idx: slice.idx,  value: $(ev.target).val() });
            }
          }, [slice.value])
        ]);
      };
    }

    // register callback on update events to UI
    this.register = function(cb) {
      listeners.push(cb);
    };

    this.dispose = function() {
      while(listeners.length > 0)
        listeners.shift();
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
