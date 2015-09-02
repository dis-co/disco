/**
 * 
 */

var _     = require('underscore');
var Base  = require('./base.js');
var async = require('async');
var Radio = require('backbone.radio');

var Log = Base.Model.extend({
  defaults: {
    ID: null,
    IP: null,
    LogLevel: null,
    Message: null,
    Role: null,
    Tag: null
  }
});

var Logs = Base.Collection.extend({
  url: 'iris.logging',
  model: Log
});

module.exports.Collection = Logs;
