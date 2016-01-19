window.IrisPlugins = window.IrisPlugins || [];

(function(plugins) {

  var ValuePlug = function(cb) {
    var h = virtualDom.h;

    function sliceView(iobox, cb) {
      return function(slice) {
        return h('div', [
          h('strong', [slice.idx]),
          h('input', {
            type: 'number',
            value: slice.value,
            onchange: function (ev) {
              iobox.slices[slice.idx] = {
                idx: slice.idx,
                value: $(ev.target).val()
              };
              cb(iobox);
            }
          }, [slice.value])
        ]);
      };
    }

    this.dispose = function() {
      while(listeners.length > 0)
        listeners.shift();
    };

    // render is expected to return a VTree even for static things like canvas
    this.render = function (iobox) {
      var view = sliceView(iobox, cb);
      return h('div', iobox.slices.map(view));
    };
  };

  plugins.push({
    name: "Value Plugin",
    type: "value",
    create: function(cb) {
      return new ValuePlug(cb);
    }
  });
})(window.IrisPlugins);
