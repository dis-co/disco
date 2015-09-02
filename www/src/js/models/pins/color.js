/**
 * <pre>
 *   ____      _            
 *  / ___|___ | | ___  _ __ 
 * | |   / _ \| |/ _ \| '__|
 * | |__| (_) | | (_) | |   
 *  \____\___/|_|\___/|_|   
 * </pre>
 *
 */
var _          = require('underscore');
var Backbone   = require('backbone');
var Base       = require('./base.js');
var Collection = require('../base.js').Collection;

/**
 * Represent a Slice of RBGA color space.
 */
var Slice = Backbone.Model.extend({
  toString: function() {
    return "rgba(" +
      this.get('r') + "," +
      this.get('g') + "," +
      this.get('b') + "," +
      this.get('a') +
      ")";
  },

  getColor: function(color) {
    var vec4 = color.toRgb();
    var out  =
          (vec4.r / 256) + ',' +
          (vec4.g / 256) + ',' +
          (vec4.b / 256) + ',' +
          vec4.a;
    return out;
  },

  setColor: function(color) {
    var parsed = color.split(',');
    this.set({
      r: parseFloat(parsed[0], 10) * 255,
      g: parseFloat(parsed[1], 10) * 255,
      b: parseFloat(parsed[2], 10) * 255,
      a: parseFloat(parsed[3], 10)
    });
  }
});

var ColorPin = Base.Model.extend({
  /**
   * ### updateAt
   * @param {integer} index - index to the color value to update (starts with 0)
   * @param {object} channels - color value object
   *
   * Updates a color value on a pin on a specific index. Since a single color
   * value occupies 4 slices on the pin, this convenience method allows us to
   * specify the color value without adding up the color channels offset each
   * time. 
   */
  updateAt: function(idx, channels) {
    var values = this.get("Values");
    values[idx] = { Key: this.get('Behavior'), Value: channels };
    this.save({ Values: values });
  },

  /**
   * ### getAt
   * @param {integer} index - get color value at index
   *
   * Return a color value at index.
   */
  getAt: function(idx) {
    return this.get("Values")[idx].Value;
  },

  /**
   * ### getSlices
   * 
   * Create and return an intermediate collection with parsed color values.
   */
  getSlices: function() {
    var type = this.behavior();
    return new Collection(_.map(this.get("Values"), function(item, idx) {

      var model = new Slice({ index: idx });
      model.setColor(item.Value);

      model.pin = this;
      return model;
    }, this));
  }
});

module.exports = ColorPin;
