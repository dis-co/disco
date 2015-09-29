window.IrisPlugins = window.IrisPlugins || [];

(function(plugins) {
  var myplugin = function(el, pin) {
    if(typeof el === 'undefined' ||
       typeof pin === 'undefined')
      throw "Must specify $el and pin to constructor";

    this._root = el;
    this._id = pin.Id;
    this._name = pin.Name;
    this._values = pin.Slices;
  };

  myplugin.prototype.get = function(prop) {
    return this["_" + prop];
  };

  myplugin.prototype.set = function(prop, val) {
    this["_" + prop] = val;
    this.trigger('set');
  };

  myplugin.prototype.onUpdate = function(cb) {
    this.on('update', function () {
      cb.apply(arguments, this);
    });
  };

  myplugin.prototype.dispose = function() {
    // cleanup listeners
  };
  
  plugins.push({
    name: "test-plugin",
    type: "number",
    constructor: myplugin
  });
})(window.IrisPlugins);
