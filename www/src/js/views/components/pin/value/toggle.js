var _          = require('underscore');
var Base       = require('../base.js');
var Backbone   = require('backbone');
var Marionette = require('backbone.marionette');

var Slice = Base.Slice.extend({
  template: require('../templates/value/toggle.hbs'),

  className: 'checkbox',

  events: {
    'change input[name="toggle"]': 'onToggle'
  },

  templateHelpers: function() {
    return this.model.attributes;
  },

  initialize: function(options) {
    this.pin = this.model.get('pin');
    this.listenTo(this.pin, 'iris.updates', function() {
      this.model.set({ Value: this.pin.getAt(this.model.get('index')) });
      this.toggle();
    });
  },

  onToggle: function() {
    this.pin.updateAt(this.model.get('index'), this.$toggle().checkbox('isChecked'));
  },

  $toggle: function() {
    if(typeof this._$toggle === 'undefined')
      this._$toggle = this.$el.find('input');
    return this._$toggle;
  },

  toggle: function() {
    if(this.model.get('Value')) {
      this.$toggle().checkbox('check');
    } else {
      this.$toggle().checkbox('uncheck');
    }
  },

  onShow: function() {
    this.toggle();
  }
});

/**
 * <pre>
 *  _____                 _      
 * |_   _|__   __ _  __ _| | ___ 
 *   | |/ _ \ / _` |/ _` | |/ _ \
 *   | | (_) | (_| | (_| | |  __/
 *   |_|\___/ \__, |\__, |_|\___|
 *            |___/ |___/        
 * </pre>
 */
var Toggle = Base.Pin.extend({
  childView: Slice,

  initialize: function(options) {
    var values = _.map(this.model.get('Values'), function(val, idx) {
      return _.extend({ index: idx, pin: this.model }, val);
    }, this);
    this.collection = new Backbone.Collection(values);
  }
});

module.exports = Toggle;
