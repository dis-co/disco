var _     = require('underscore');
var Radio = require('backbone.radio');

/**
 * Patch Marionette so we can use Radio instead of Wreqr (soon to be replaced anyways);
 */
require('backbone.marionette').Application.prototype._initChannel = function() {
  this.channelName = _.result(this, 'channelName') || 'global';
  this.channel = _.result(this, 'channel') || Radio.channel(this.channelName);
};
