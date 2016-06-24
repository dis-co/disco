//  ____                _
// |  _ \ ___  ___  ___| |_
// | |_) / _ \/ __|/ _ \ __|
// |  _ <  __/\__ \  __/ |_
// |_| \_\___||___/\___|\__|

window.resetPlugins = function() {
  window.IrisPlugins = [];
};

//  ____  _        _
// / ___|| |_ _ __(_)_ __   __ _
// \___ \| __| '__| | '_ \ / _` |
//  ___) | |_| |  | | | | | (_| |
// |____/ \__|_|  |_|_| |_|\__, |
//                         |___/

window.simpleString1 = function() {
  var h = virtualDom.h;

  var stringplugin = function(cb) {
    this.render = function (iobox) {
      return h("div", { id: iobox.Id }, iobox.Slices.Map(function(slice) {
        return h("input", {
          value: slice.Value,
          className: "slice",
          onchange: function(ev) {
            // strive for immutability
            var newslice = iobox.Slices.CreateString(slice.Index, ev.target.value);
            var newbox = iobox.SetSlice(newslice);
            cb(newbox);
          }
        }, [ slice.Value ]);
      }));
    };
    this.dispose = function() {};
  };

  window.IrisPlugins.push({
    name: "simple-string-plugin",
    type: "StringPin",
    create: function(cb) {
      return new stringplugin(cb);
    }
  });
};

//      _        _
//  ___| |_ _ __(_)_ __   __ _
// / __| __| '__| | '_ \ / _` |
// \__ \ |_| |  | | | | | (_| |
// |___/\__|_|  |_|_| |_|\__, |
//                       |___/

window.simpleString2 = function() {
  var h = virtualDom.h;
  
  var sliceView = function(slice) {
    return h('li', [
      h('input', {
        className: 'slice',
        type: 'text',
        name: 'slice',
        value: slice.Value
      }, [ slice.Value ])
    ]);
  };

  var slices = function (iobox) {
    return h('ul', {
      className: 'slices'
    }, iobox.Slices.Map(sliceView));
  };

  // plugin constructor
  var myplugin = function() {

    // update view
    this.render = function (iobox) {
      return h('div', {
        id: iobox.Id
      }, [
        h("p", { className: 'name' }, [ iobox.Name ]),
        slices(iobox)
      ]);
    };

    this.dispose = function() {
    };
  };

  window.IrisPlugins.push({
    name: "simple-string-plugin",
    type: "string",
    create: function() {
      return new myplugin(arguments);
    }
  });
};

//  _   _                 _
// | \ | |_   _ _ __ ___ | |__   ___ _ __
// |  \| | | | | '_ ` _ \| '_ \ / _ \ '__|
// | |\  | |_| | | | | | | |_) |  __/ |
// |_| \_|\__,_|_| |_| |_|_.__/ \___|_|

window.numberPlugin = function() {
  var h = virtualDom.h;
  
  var numberplugin = function(cb) {
    this.render = function (iobox) {
      var view = h("div", { id: iobox.Id }, [
        h("p", { className: "slice" }, [ iobox.Slices.At(0).Value ])
      ]);
      return view;
    };
    this.dispose = function() {};
  };

  window.IrisPlugins.push({
    name: "simple-number-plugin",
    type: "ValuePin",
    create: function(cb) {
      return new numberplugin(cb);
    }
  });
};
