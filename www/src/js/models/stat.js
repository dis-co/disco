/**
 * <pre>
 *      ___           ___           ___           ___     
 *     /\  \         /\  \         /\  \         /\  \    
 *    /::\  \        \:\  \       /::\  \        \:\  \   
 *   /:/\ \  \        \:\  \     /:/\:\  \        \:\  \  
 *  _\:\~\ \  \       /::\  \   /::\~\:\  \       /::\  \ 
 * /\ \:\ \ \__\     /:/\:\__\ /:/\:\ \:\__\     /:/\:\__\
 * \:\ \:\ \/__/    /:/  \/__/ \/__\:\/:/  /    /:/  \/__/
 *  \:\ \:\__\     /:/  /           \::/  /    /:/  /     
 *   \:\/:/  /     \/__/            /:/  /     \/__/      
 *    \::/  /                      /:/  /                 
 *     \/__/                       \/__/                  
 * </pre>
 *
 * Model and collection for retieving statistical data for _VVVV_ host.
 */
var _    = require('underscore');
var Base = require('./base.js');

/**
 * ### Model
 */
var Stat = Base.Model.extend({
  url: 'iris.host.stat'
});

/**
 * ### Collection
 */
var Stats = Base.Collection.extend({
  url: 'iris.host.stats',
  model: Stat,

  wrapCount: 20,

  /**
   * ### initialize
   *
   * Add an event handler that throws out stats after the collection has grown
   * to a certain size to keep memory usage constant.
   */
  initialize: function() {
    this.on('add', _.bind(function(mode) {
      if(this.size() >= this.wrapCount) this.shift();
    }, this));
  }
});

module.exports.Model      = Stat;
module.exports.Collection = Stats;
