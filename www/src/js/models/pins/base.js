/**
 * <pre>
 *       ___                       ___     
 *      /\  \          ___        /\__\    
 *     /::\  \        /\  \      /::|  |   
 *    /:/\:\  \       \:\  \    /:|:|  |   
 *   /::\~\:\  \      /::\__\  /:/|:|  |__ 
 *  /:/\:\ \:\__\  __/:/\/__/ /:/ |:| /\__\
 *  \/__\:\/:/  / /\/:/  /    \/__|:|/:/  /
 *       \::/  /  \::/__/         |:/:/  / 
 *        \/__/    \:\__\         |::/  /  
 *                  \/__/         /:/  /   
 *                                \/__/    
 * </pre>
 *
 * Base protoype for different Pin models. 
 */
var _    = require('underscore');
var Base = require('../base.js');

/**
 * <pre>
 *  __  __           _      _ 
 * |  \/  | ___   __| | ___| |
 * | |\/| |/ _ \ / _` |/ _ \ |
 * | |  | | (_) | (_| |  __/ |
 * |_|  |_|\___/ \__,_|\___|_|
 * </pre>
 *
 * All Pin values share common properties, such as the idAttribute, url
 * endpoint, certain _Channels_ actions and methods.
 */
var Pin = Base.Model.extend({
  idAttribute: 'Id',

  url: 'iris.pins',

  /**
   * ### _selected & _selectable
   *
   * _All your state are belong to us._
   * 
   * Pins need to remember when they have been selected on a view, so that they
   * can decide whether or not to respond with data when a `save` is being
   * requested in the cue editor.
   */ 
  _selected:   false,
  _selectable: false,

  live: true,

  channels: {
    pins: 'pins',
    cues: 'cues'
  },

  /**
   * ### actions
   *
   * When _creating_ Cues, Pins need to respond to become selectable, should
   * respond to `cancel` events, and return data when `save` is called. Also,
   * when Cues are being _edited_, we respond to the `select` command to set
   * this Pin selectable and select it.
   */
  actions: {
    'event/cues/selectable': function() {
      this._selectable = true;
      this._selected   = false;
      this.trigger('selectable');
    },
    'event/cues/select/:id': function(hostid) {
      if(!this._selected) {
        this._selected   = true;
        this._selectable = true;
        this.trigger('select');
      }
    },
    'event/cues/deselect/:id': function(hostid) {
      if(this._selected) {
        this._selected   = false;
        this._selectable = true;
        this.trigger('select');
      }
    },
    'event/cues/set/:id': function(values) {
      this.saveValues(_.map(values, function(val) {
        return val.Value;
      }, this), { push: false });
    },
    'request/cues/save/:id': function() {
      if(this._selectable && this._selected) {
        return this.toCue();
      }
      return null;
    },
    'event/cues/cancel': function() {
      this._selectable = false;
      this._selected   = false;
      this.trigger('cancel');
    },
    'event/pins/live': function(val) {
      this.live = val;
    },
    'event/pins/internal/:id': function (host, values) {
      if(host === this.get('HostId')) return;
      if(this.behavior() === 'Bang')  return;
      this.set({ Values: values}, { push: false });
      this.trigger('iris.updates');
    }
  },

  initialize: function (arg) {
    this.listenTo(this, 'request', function () {
      this.pins.trigger('internal/' + this.id,
                        this.get('HostId'),
                        this.get('Values'));
    });
  },

  /**
   * ### selectable
   *
   * Get: is this Pin selectable?
   */
  selectable: function() {
    return this._selectable;
  },

  /**
   * ### selected
   * @param {boolean} selected - selected state
   *
   * Get/set: if passed an argument, set the value of `this._selected` to
   * `bool`, else return the proptery's current value.
   */ 
  selected: function(bool) {
    if(typeof bool === 'undefined') {
      // get state
      return this._selected;
    }
    // set state
    this._selected = bool;

    if(bool) this.cues.trigger('select/'   + this.id);
    else     this.cues.trigger('deselect/' + this.id);

    return bool;
  },

  /**
   * ### toggleSelected
   *
   * Set: toggle the current state of selectability of this Pin.
   */
  toggleSelected: function() {
    if(this._selected) {
      this._selected = false;
    } else {
      this._selected = true;
    }
  },

  
  /**
   * ### type
   *
   * Return the current Pin type as string, by way of indexing into the base
   * TYPES constant array. The array order corresponds to numbers assigned
   * Iris-side PinType enumeration.
   */
  type: function() {
    return this.get('Type') + 'Pin';
  },

  /**
   * ### behavior
   *
   * Return the current Pin behavior as string, by way of indexing into the base
   * BEHAVIORS constant. The array order corresponds to numbers assigned
   * Iris-side Behavior enumeration.
   */
  behavior: function() {
    return this.get('Behavior');
  },

  /**
   * ### valueType
   *
   * Return the value type of this pin. Only relevant for Pins of type `Value`.
   */
  valueType: function() {
    return this.get('ValueType');
  },

  /**
   * ### toCue
   *
   * Get: serialize the current state of this Pin to a Cue-compatible format.
   */
  toCue: function() {
    return {
      Type: 0, // IOPin
      Target: this.id,
      Values: this.get('Values')
    };
  },

  /**
   * ### updateAt
   * @param {integer} index - index into the Values array 
   * @param {object} value - value to save
   *
   * Set: update the pin values array at the specified index, with the supplied
   * value.
   */
  updateAt: function(idx, value, silent) {
    var values = this.get("Values");
    values[idx] = { Behavior: this.get('Behavior'), Value: value };

    this.pins.trigger('internal/'+this.id, this.get('HostId'), values);

    if(!silent) this.save({
      Values: values
    }, {
      push: this.live
    });
  },

  /**
   * ### getAt
   * @param {integer} index - index to value to get
   *
   * Get: the slice value at the specified index.
   */
  getAt: function(idx) {
    return this.get("Values")[idx].Value;
  },

  /**
   * ### saveValues
   * @param {array} values - values to save to model `Values` property
   */
  saveValues: function(values, opts) {
    this.save({
      Values: _.map(values, function(value) {
        return { Behavior: this.get('Behavior'), Value: value };
      }, this)
    }, _.extend({ push: this.live }, opts));
    this.trigger('iris.updates');
  }
});

module.exports.Model    = Pin;
