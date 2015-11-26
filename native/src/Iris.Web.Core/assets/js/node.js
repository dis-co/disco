window.IrisPlugins = window.IrisPlugins || [];

(function(plugins) {
  var h = virtualDom.h;

  function sliceView(slice) {
    return h('div', [
      h('h3', ['Slice: ' + slice.idx]),
      h('input', {
        onchange: function (ev) {
          console.log("onchange!");
        }
      }, [slice.value])
    ]);
  }
  
  var NodePlug = function () {
    this.dispose = function() {
      console.log("dispose called");
    };

    // render is expected to return a VTree even for static things like canvas
    this.render = function (iobox) {
      return h('div', iobox.slices.map(sliceView));
    };
  };
  
  plugins.push({
    name: "Node Plugin",
    type: "node",
    create: function() {
      return new NodePlug(arguments);
    }
  });
})(window.IrisPlugins);
