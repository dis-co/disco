var _        = require('underscore');
var Base     = require('../base.js');
var Backbone = require('backbone');

/**
 * <pre>
 *  ____                    
 * | __ )  __ _ _ __   __ _ 
 * |  _ \ / _` | '_ \ / _` |
 * | |_) | (_| | | | | (_| |
 * |____/ \__,_|_| |_|\__, |
 *                    |___/ 
 * </pre>
 */
var Slice = Base.Slice.extend({
  template: require('../templates/value/bang.hbs'),

  events: {
    'click button.bang': 'bang'
  },

  ui: {
    'bang': 'button.bang'
  },

  initialize: function() {
    this.pin = this.model.get('pin');
    this.listenTo(this.pin, 'iris.updated', _.bind(function(values) {
      if(values[this.model.get('index')])
        this.flash();
    },this));
  },

  bang: function(event) {
    event.preventDefault();
    // value: true, silent: false
    this.pin.updateAt(this.model.get('index'), true);
    this.flash();
  },

  flash: function() {
    this.ui.bang.addClass('btn-danger');
    setTimeout(_.bind(function() {
      this.ui.bang.removeClass('btn-danger');
    },this), 100);
  }
});

var Bang = Base.Pin.extend({
  childView: Slice,

  initialize: function(options) {
    var values = _.map(this.model.get('Values'), function(value, idx) {
      return _.extend({
        index: idx,
        pin: this.model
      });
    }, this);
    this.collection = new Backbone.Collection(values);
  }
});

module.exports = Bang;
