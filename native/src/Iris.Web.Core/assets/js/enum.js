window.IrisPlugins = window.IrisPlugins || [];

(function(plugins) {
  var h = virtualDom.h;

  function sliceView(iobox, cb) {
    return function(slice) {
      return h('div', [
        h('h3', ['Slice: ' + slice.idx]),
        h('input', {
          onchange: function (ev) {
            iobox.slices[slice.idx] = { idx: slice.idx, value: ev.target.value };
            cb(iobox);
          }
        }, [slice.value])
      ]);
    };
  }
  
  var EnumPlug = function(cb) {
    this.dispose = function() {
      console.log("dispose called");
    };

    // render is expected to return a VTree even for static things like canvas
    this.render = function (iobox) {
      var view = sliceView(iobox, cb);
      return h('div', iobox.slices.map(view));
    };
  };
  
  plugins.push({
    name: "Enum Plugin",
    type: "enum",
    create: function(cb) {
      return new EnumPlug(cb);
    }
  });
})(window.IrisPlugins);
