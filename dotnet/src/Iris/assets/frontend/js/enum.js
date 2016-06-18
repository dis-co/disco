window.IrisPlugins = window.IrisPlugins || [];

(function(plugins) {
  var h = virtualDom.h;

  function sliceView(iobox, cb) {
    var options = function(selected) {
      return iobox.properties.map(function(prop) {
        var opts = { value: prop[0] };
        if(prop[0] == selected) opts['selected'] = 'selected';
        return h('option', opts, [ prop[1] ]);
      });
    };

    return function(slice) {
      return h('div', [
        h('h3', ['Slice: ' + slice.idx]),
        h('select', {
          onchange: function (ev) {
            iobox.slices[slice.idx] = {
              idx: slice.idx,
              value: $(ev.target).val()
            };
            cb(iobox);
          }
        }, options(slice.value))
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
