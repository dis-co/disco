/**
 * <pre>
 *            ___           ___           ___           ___
 *           /\  \         /\  \         /\  \         /\  \
 *          /::\  \ are   /::\  \       /::\  \       /::\  \
 *         /:/\:\  \     /:/\:\  \     /:/\ \  \ to  /:/\:\  \
 *        /::\~\:\__\   /::\~\:\  \   _\:\~\ \  \   /::\~\:\  \
 *       /:/\:\ \:|__| /:/\:\ \:\__\ /\ \:\ \ \__\ /:/\:\ \:\__\
 *       \:\~\:\/:/  / \/__\:\/:/  / \:\ \:\ \/__/ \:\~\:\ \/__/
 * ALL    \:\ \::/  /       \::/  /   \:\ \:\__\    \:\ \:\__\
 *   your  \:\/:/  /        /:/  /     \:\/:/  /     \:\ \/__/
 *          \::/__/        /:/  /       \::/  /       \:\__\
 *           ~~            \/__/ belong  \/__/         \/__/ us!
 * </pre>
 */
var Backbone   = require('backbone');
var Channels   = require('../lib/channels.js');

/**
 * Add _Channels_ to Backbone Models, as well as a default callback for events
 * on `transport` channel.
 */
var Model = Channels.infect(Backbone.Model).extend({
  actions: {
    'event/transport/connected': function(data) {
      this.reset();
      this.fetch();
    }
  }
});

/**
 * Add _Channels_ to Backbone Collections, as well as a default callback for
 * events on `transport` channel.
 */
var Collection = Channels.infect(Backbone.Collection).extend({
  actions: {
    'event/transport/connected': function(data) {
      this.reset();
      this.fetch();
    }
  }
});

module.exports.Model      = Model;
module.exports.Collection = Collection;
