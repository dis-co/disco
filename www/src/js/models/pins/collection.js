/**
 * <pre>
 *   ____      _ _           _   _             
 *  / ___|___ | | | ___  ___| |_(_) ___  _ __  
 * | |   / _ \| | |/ _ \/ __| __| |/ _ \| '_ \ 
 * | |__| (_) | | |  __/ (__| |_| | (_) | | | |
 *  \____\___/|_|_|\___|\___|\__|_|\___/|_| |_|
 * </pre>
 *
 * Fetch and parse exposed Pins from a running VVVV patch.
 */
var _          = require('underscore');
var Base       = require('./base.js');
var Collection = require('../base.js').Collection;

var StringPin = require('./string.js');
var ColorPin  = require('./color.js');
var EnumPin   = require('./enum.js');
var ValuePin  = require('./value.js');

var getModel = function(attrs, options) {
  switch(attrs.Type) {
  case 'String':
    return new StringPin(attrs, options);
  case 'Color':
    return new ColorPin(attrs, options);
  case 'Enum':
    return new EnumPin(attrs, options);
  default:
    return new ValuePin(attrs, options);
  }
};

/**
 * Instantiate new models according types mapped out server-side by the PinType
 * enumeration, and client-side in `pins/base.js`.
 */
var Pins = Collection.extend({
  url: 'iris.pins',

  actions: {
    'event/push/create/:url': function(data) {
      if(this.patch && this.patch.get("HostId") == data.HostId)
        this.add(getModel(data));
    }
  },
  
  model: getModel 
});

module.exports = Pins;
