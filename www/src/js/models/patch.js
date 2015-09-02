/**
 * <pre style="width:800px">
 *      ___           ___           ___           ___           ___     
 *     /\  \         /\  \         /\  \         /\  \         /\__\    
 *    /::\  \       /::\  \        \:\  \       /::\  \       /:/  /    
 *   /:/\:\  \     /:/\:\  \        \:\  \     /:/\:\  \     /:/__/     
 *  /::\~\:\  \   /::\~\:\  \       /::\  \   /:/  \:\  \   /::\  \ ___ 
 * /:/\:\ \:\__\ /:/\:\ \:\__\     /:/\:\__\ /:/__/ \:\__\ /:/\:\  /\__\
 * \/__\:\/:/  / \/__\:\/:/  /    /:/  \/__/ \:\  \  \/__/ \/__\:\/:/  /
 *      \::/  /       \::/  /    /:/  /       \:\  \            \::/  / 
 *       \/__/        /:/  /     \/__/         \:\  \           /:/  /  
 *                   /:/  /                     \:\__\         /:/  /   
 *                   \/__/                       \/__/         \/__/    
 * </pre>
 *
 * A group of exposed _IOPins_ in _VVVV_.
 */
var Base     = require('./base.js');
var Pin      = require('./pins');
var Settings = require('../app/settings.js');

var _        = require('underscore');

/**
 * <pre>
 *  __  __           _      _ 
 * |  \/  | ___   __| | ___| |
 * | |\/| |/ _ \ / _` |/ _ \ |
 * | |  | | (_) | (_| |  __/ |
 * |_|  |_|\___/ \__,_|\___|_|
 * </pre>
 *
 * Connect to `iris.patch` endpoint and fetch patch metadata and pins.
 */
var Patch = Base.Model.extend({
  url: 'iris.patches', 

  idAttribute: 'Id',

  /**
   * ### parse
   * @param {object} resp - patch model attribute object
   *
   * Construct a collection for the pins in this patch and delete the array from
   * the attributes hash, as we won't need it anymore.
   */
  parse: function(resp) {
    if(resp) {
      this.pins = new Pin.Collection(resp.Pins);
      this.pins.patch = this;
      delete resp.Pins;
    }
    return resp;
  }
});

/**
 * <pre>
 *   ____      _ _           _   _             
 *  / ___|___ | | | ___  ___| |_(_) ___  _ __  
 * | |   / _ \| | |/ _ \/ __| __| |/ _ \| '_ \ 
 * | |__| (_) | | |  __/ (__| |_| | (_) | | | |
 *  \____\___/|_|_|\___|\___|\__|_|\___/|_| |_|
 * </pre>
 *
 * Fetch all patches in an open project.
 */
var Patches = Base.Collection.extend({
  url: 'iris.patches',

  comparator: 'HostName',
  
  model: Patch,

  initialize: function() {
    this.host = Settings.get('hostlist-selection') || 'All';
  },

  /**
   * ### actions
   *
   * Register a global request handler to dish-out pin IDs. This is used during
   * Cue-creation/edit time.
   */
  actions: {
    'request/pins/ids': function() {
      return this.getPinIDs();
    }
  },

  /**
   * ### getPinIDs
   *
   * Get: map to IDs over all pins in all patches.
   */
  getPinIDs: function() {
    return _.flatten(this.map(function(patch) {
      return patch.pins.map(function(pin) {
        return { host: patch.get('HostId'), pin: pin.id };
      });
    }));
  },

  setHost: function(name) {
    this.host = name;
    this.reset();
    this.fetch();
  }
});

module.exports.Model      = Patch;
module.exports.Collection = Patches;
