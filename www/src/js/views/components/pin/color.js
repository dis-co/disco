var _    = require('underscore');
var Base = require('./base.js');

var Slice = Base.Slice.extend({
  tagName: 'input',
  className: 'colorpicker',
  template: false,

  initialize: function(options) {
    this.listenTo(this.model.pin, 'iris.updates', function() {
      var value = this.model.pin
            .getAt(this.model.get('index'));
      this.model.setColor(value);
      this.$el.spectrum('set', this.model.toString()); 
    });
  },

  onShow: function() {
    this.$el.spectrum({
      showAlpha: true,
      change: _.bind(function(color) {
        var idx = this.model.get('index');
        this.model.pin.updateAt(idx, this.model.getColor(color));
      },this)
    });
    this.$el.spectrum('set', this.model.toString()); 
  }
});

var Color = Base.Pin.extend({
  childView: Slice,

  initialize: function(options) {
    this.collection = this.model.getSlices();
  },

  buildChildView: function(child, View, childViewOptions) {
    var options = _.extend({ model: child }, childViewOptions);
    var view = new View(options);
    return view;
  }
});

// ### IMPORTANT! ###
//
// The names exported here directly map to Color Pin Behaviors (see
// [models/pin.js](../../models/pin.js)), so be sure to change them everywhere
// they are used! Otherwise you might end up with hard-to-find bugs at runtime.
// Javascript, don't you love it?
module.exports.None = Color;      
