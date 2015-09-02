var _        = require('underscore');
var Base     = require('../base.js');
var Backbone = require('backbone');

var Slice = Base.Slice.extend({
  template: require('../templates/value/xslider.hbs'),

  initialize: function(options) {
    this.pin = this.model.get('pin');
    // listen to changes in the values array on the model
    this.listenTo(this.pin, 'iris.updates', function(m) {
      var val = this.pin.getAt(this.model.get('index'));
      this.set(val);
    });
  },

  onRender: function() {
    this.number().hide();
    this.slider().hide();
  },

  set: function(val) {
    if(this.pin.get('ShowValue')) {
      this.number().spinner('value', val);
    }
    
    if(this.pin.get('ShowSlider')) {
      this.slider().slider({ value: val });
    }
  },
  
  slider: function() {
    if(typeof this.$slider === 'undefined')
      this.$slider = this.$el.find('.slider');
    return this.$slider;
  },

  number: function() {
    if(typeof this.$number === 'undefined')
      this.$number = this.$el.find('.amount');
    return this.$number;
  },

  onShow: function() {
    var showValue  = this.pin.get('ShowValue');
    var showSlider = this.pin.get('ShowSlider');
    
    var min = this.pin.get('MinValue');
    var max = this.pin.get('MaxValue');

    var index = this.model.get('index');
    var value = this.model.get('Value');

    if(showValue) {
      var cb = _.bind(function() {
        var val = this.number().spinner('value');
        if(val === null) {
          this.number().parent().css('border', '1px solid red');
        } else {
          this.number().parent().css('border', 'none');
          this.pin.updateAt(index, val);
        }
      },this);

      this.number().spinner({
        min: min,
        max: max,
        spin: cb,
        stop: cb,
        step: this.precision()
      });
      
      this.number().spinner('value', value);
      this.number().show();
    }

    if(showSlider) {
      this.slider().slider({
        min: min,
        max: max,
        value: value,
        slide: _.bind(function( event, ui ) {
          this.pin.updateAt(index, ui.value);
          this.set(ui.value);
        },this)
      });
      this.slider().show();
    }
  },

  precision: function() {
    var type = this.model.get('pin').valueType();
    if(type === 'Real') {
      var prec = this.model.get('pin').get('Precision');
      return (prec === 0)
        ?  1
        : (1 / Math.pow(10 ,prec));
    }
    return 1;
  }
});

/**
 *
 * <pre>
 * __  ______  _ _     _
 * \ \/ / ___|| (_) __| | ___ _ __
 *  \  /\___ \| | |/ _` |/ _ \ '__|
 *  /  \ ___) | | | (_| |  __/ |
 * /_/\_\____/|_|_|\__,_|\___|_|
 * </pre>
 *
 */
var XSlider = Base.Pin.extend({
  childView: Slice,

  initialize: function(options) {
    var values = _.map(this.model.get('Values'), function(value, idx) {
      return _.extend({ pin: this.model, index: idx }, value);
    }, this);
    this.collection = new Backbone.Collection(values);
  }
});

module.exports = XSlider;
