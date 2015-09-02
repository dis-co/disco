var _        = require('underscore');
var Backbone = require('backbone');
var Radio    = require('backbone.radio');
var channel  = Radio.channel('pull://');

/**
 * <pre>
 * __     __    _            
 * \ \   / /_ _| |_   _  ___ 
 *  \ \ / / _` | | | | |/ _ \
 *   \ V / (_| | | |_| |  __/
 *    \_/ \__,_|_|\__,_|\___|
 * </pre>
 *
 * A little value container with 'with' setter for nice semantics :)
 */

var Value = function(id) {
  this.id = id;
};

Value.prototype.with = function(val) {
  this.value = val;
};

/**
 * <pre>
 * __     __        _     
 * \ \   / /__ _ __| |__  
 *  \ \ / / _ \ '__| '_ \ 
 *   \ V /  __/ |  | |_) |
 *    \_/ \___|_|  |_.__/ 
 * </pre>
 *
 * Reprends a CRUD verb, and id/value pairs for objects reachable under this
 * verb on the resource.
 *
 * Usage:
 * <code>
 *   var read = new Verb('read');
 *   create.on('my-fake-obj-id').with({ name: "nice", age: 23 });
 * </code>
 */

var Verb = function(name) {
  this.name = name;
  this.values = {};
};

Verb.prototype.on = function(id) {
  this.values[id] = this.values[id] || new Value(id);
  return this.values[id];
};

Verb.prototype.with = function(val) {
  var id = (Math.random() * new Date().getTime()).toString();
  var value = new Value(id);
  value.with(val);
  this.values[id] = value;
};

/**
 * <pre>
 * ____                                    
 * |  _ \ ___  ___  ___  _   _ _ __ ___ ___ 
 * | |_) / _ \/ __|/ _ \| | | | '__/ __/ _ \
 * |  _ <  __/\__ \ (_) | |_| | | | (_|  __/
 * |_| \_\___||___/\___/ \__,_|_|  \___\___|
 * </pre>
 *
 * Create a fake resource declaratively, by specifying the expected responses
 * for a given CRUD verb and payload.
 * 
 * Usage:
 * <code>
 *   var resource = new Resource('yes-its-true');
 *   resource.respondTo('read').on('my-obj-id').with({ name: 'karsten', age: 23 });
 * </code>
 *
 * From there on out, a model with this URI set to the corresponding resource
 * endpoint and id, will contain the specified values when fetched.
 */
var Resource = function(resource, options) {
  this._pull = Radio.channel('pull');
  this._push = Radio.channel('push');
  this._callbacks = {};
  this.resource = resource;
  this.options  = options;
};

Resource.prototype.respondTo = function(verb) {
  var resource = this.resource;
  this._callbacks[verb] = this._callbacks[verb] || new Verb(verb);

  this._pull.reply(verb, function(url, model, callback) {
    if(url != resource) {
      callback('resource not found');
      return;
    }

    var response;
    // simulates real work
    switch(verb) {
    case 'create':
      response = _.extend(model, this.values[Object.keys(this.values)[0]].value);
      break;
    case 'read':
      if(model && model.id) {
        // if model attributes were passed 
        response = this.values[model.id]
          ? _.extend(model, this.values[model.id].value)
          : null;
      } else {
        // its a collection...
        response = this.values[Object.keys(this.values)[0]].value;
      }
      break;
    case 'update':
      response = this.values[model.id]
        ? model.attributes
        : null;
      break;
    case 'delete':
      response = this.values[model.id]
        ? model
        : null;
      break;
    }

    if(response) {
      callback(null, response);
    } else {
      callback('not found');
    }
  }, this._callbacks[verb]);

  return this._callbacks[verb];
};

Resource.prototype.pushTo = function(verb, payload) {
  var url = this.resource + '/' + verb;
  switch(verb) {
  case 'create':
    this._push.trigger(url, payload);
  default:
    this._push.request(url, payload);
  }
};

Resource.prototype.dispose = function() {
  this._pull.reset();
  this._push.reset();
};


module.exports = Resource;
