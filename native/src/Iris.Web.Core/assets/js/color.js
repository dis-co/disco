window.IrisPlugins = window.IrisPlugins || [];

(function(plugins) {
  var h = virtualDom.h;

  function sliceView(iobox, cb) {
    return function(slice) {
      return h('div', [
        h('h3', ['Slice: ' + slice.idx]),
        h('input', {
          type: 'color',
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
  
  var ColorPlug = function (cb) {
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
    name: "Color Plugin",
    type: "color",
    create: function(change) {
      return new ColorPlug(change);
    }
  });
})(window.IrisPlugins);
