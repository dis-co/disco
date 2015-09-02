var _        = require('underscore');
var Base     = require('../base.js');
var Backbone = require('backbone');
var TwoDee   = require('./twodee.js');

var Slice = Base.Slice.extend({
  className: 'xyslider',

  template: require('../templates/value/xyslider.hbs'),

  initialize: function(options) {
    this.pin = this.model.get('pin');

    this.listenTo(this.pin,'iris.updates', function(m) {
      var idx   = this.model.get('index');
      var value = this.pin.get('Values')[idx].Value.split('x');
      this.twodee.set({
        x: parseFloat(value[0],10),
        y: parseFloat(value[1],10)
      });
    });
  },

  onShow: function() {
    this.twodee = new TwoDee({
      el:     this.$el.find('.slider'),
      width:  200,
      height: 200,
      color:  '#eee',
      min:    this.pin.get('MinValue'),
      max:    this.pin.get('MaxValue'),
      handle: { size: 12, color: '#888888' },
      onChange: _.bind(function(values) {
        var idx  = this.model.get('index');
        var vals = this.pin.get('Values');

        vals[idx] = {
          Behavior: this.pin.get('Behavior'),
          Value: values.x + 'x' + values.y
        };

        this.pin.save({ Values: vals });
      },this)
    });
    this.twodee.set({
      x: this.model.get('x'),
      y: this.model.get('y')
    });
  }
});

/**
 * <pre>
 * __  ____   ______  _ _     _           
 * \ \/ /\ \ / / ___|| (_) __| | ___ _ __ 
 *  \  /  \ V /\___ \| | |/ _` |/ _ \ '__|
 *  /  \   | |  ___) | | | (_| |  __/ |   
 * /_/\_\  |_| |____/|_|_|\__,_|\___|_|   
 * </pre>                                       
 */
var XYSlider = Base.Pin.extend({
  childView: Slice,
  
  initialize: function(options) {
    this.collection = new Backbone.Collection(
      _.map(this.model.get('Values'), function(item, idx) {
        var vals = item.Value.split('x');
        return new Backbone.Model({
          x: parseFloat(vals[0], 10),
          y: parseFloat(vals[1], 10),
          index: idx,
          pin: this.model});
      }, this));
  }
});

module.exports = XYSlider;
