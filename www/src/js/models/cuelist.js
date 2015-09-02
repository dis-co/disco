/**
 * <pre>
 *      ___           ___           ___     
 *     /\__\         /\  \         /\__\    
 *    /:/  /         \:\  \       /:/ _/_   
 *   /:/  /           \:\  \     /:/ /\__\  
 *  /:/  /  ___   ___  \:\  \   /:/ /:/ _/_ 
 * /:/__/  /\__\ /\  \  \:\__\ /:/_/:/ /\__\
 * \:\  \ /:/  / \:\  \ /:/  / \:\/:/ /:/  /
 *  \:\  /:/  /   \:\  /:/  /   \::/_/:/  / 
 *   \:\/:/  /     \:\/:/  /     \:\/:/  /  
 *    \::/  /       \::/  /       \::/  /   
 *     \/__/         \/__/         \/__/  List
 * </pre>
 *
 * A sorted list of Cue objects.
 */
var _        = require('underscore');
var Base     = require('./base.js');
var Cue      = require('./cue.js');
var Backbone = require('backbone');

/**
 * ### name
 *
 * Default name to give to new cue lists;
 */
var DEFAULT_NAME = 'Cue List';

/**
 * <pre>
 *  __  __           _      _ 
 * |  \/  | ___   __| | ___| |
 * | |\/| |/ _ \ / _` |/ _ \ |
 * | |  | | (_) | (_| |  __/ |
 * |_|  |_|\___/ \__,_|\___|_|
 * </pre>
 *
 * A named collection of references to Cue objects by way of the Cue ID.
 */
var CueList = Base.Model.extend({
  url: 'iris.cuelists',

  idAttribute: '_id',

  defaults: {
    Project: '',
    Type:    'CueList',
    Cues:    []
  }
});

/**
 * <pre>
 *            _ _           _   _           
 *   ___ ___ | | | ___  ___| |_(_) ___  _ __     
 *  / __/ _ \| | |/ _ \/ __| __| |/ _ \| '_ \    
 * | (_| (_) | | |  __/ (__| |_| | (_) | | | |  
 *  \___\___/|_|_|\___|\___|\__|_|\___/|_| |_|  
 * </pre>
 */
var CueLists = Base.Collection.extend({
  url: 'iris.cuelists',
  model: CueList,

  initialize: function(options) {
    this.project = options ? options.project : '';
  },

  /**
   * ### new
   *
   * Set: conveniently create a new CueList with a generated default name.
   */
  new: function() {
    this.create({
      Project: this.project || '',
      Name: this.getDefaultName()
    });
  },

  /**
   * ### getDefaultName
   *
   * Get: generate a default name for cue lists. Operates more or the same as
   * the corresponding method on the Cue collection. Find the highest count of
   * cue lists ending with a number, add 1 and combine with the default name.
   */
  getDefaultName: function() {
    var last = _.last(this.filter(function(cuelist) {
      return cuelist.get('Name').match(new RegExp(DEFAULT_NAME));
    }).sort());

    if(last) {
      var parsed = last.get('Name').split(" ");
      var num;
      try {
        num = parseInt(parsed[parsed.length - 1], 10);
      } catch(e) {
        num = 1;
      }
      return DEFAULT_NAME + " " + (num + 1);
    } else {
      return DEFAULT_NAME + " 1";
    }
  }
});

module.exports.Model      = CueList;
module.exports.Collection = CueLists;
