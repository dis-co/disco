/**
 * <pre>
 * __     __    _            
 * \ \   / /_ _| |_   _  ___ 
 *  \ \ / / _` | | | | |/ _ \
 *   \ V / (_| | | |_| |  __/
 *    \_/ \__,_|_|\__,_|\___|
 * </pre>
 *
 * Represent exposed _IOPins_ of type `Value`.
 */
var _    = require('underscore');
var Base = require('./base.js');

var ValuePin = Base.Model.extend({

  initialize: function (arguments) {
    Base.Model.prototype.initialize.apply(this, arguments);

    if(this.behavior() === "Bang") {
      this.listenTo(this, 'iris.updates sync', function() {
        this.trigger('iris.updated', _.map(this.get("Values"), function (val) {
          return val.Value;
        }));

        // value: false, silent: true (resetting local pin)
        _.each(this.get("Values"), function (val, idx) {
          this.updateAt(idx, false, true);
        }, this);
      });
    }
  },

  /**
   * ### anyTrue
   * Return true, if any of values are truthy.
   */
  anyTrue: function() {
    return _.reduce(this.get("Values"), function(memo,value) {
      if(!memo)
        memo = value.Value == 1;
      return memo;
    }, false);
  },

  values: function name(arg) {
    if(this.behavior() === "Bang") {
      return _.map(this.get("Values"), function (val) {
        val.Value = true;
        return val;
      });
    }
    return this.get('Values');
  },
  
  toCue: function() {
    return {
      Type: 0, // IOPin
      Target: this.id,
      Values: this.values()
    };
  }
});

module.exports = ValuePin;
