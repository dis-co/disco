/**
 * <pre>
 *  _______ __                            __       
 * |   _   |  |--.---.-.-----.-----.-----|  .-----.
 * |.  1___|     |  _  |     |     |  -__|  |__ --|
 * |.  |___|__|__|___._|__|__|__|__|_____|__|_____|
 * |:  1   |                                       
 * |::.. . |                                       
 * `-------'                                       
 * </pre>
 *
 * _Channels_ adds several capabilities to Backbone Views, Models and
 * Collections to aid development of loosely coupled, event-driven
 * architectures. _Channels_ is basically a convenience layer built on top of
 * Backbone.Radio, providing a declarative way of defining channel objects and
 * actions to subscribe to/comply with, once called.
 * 
 * The main aspects in which the _Channels_ mixin alters the default behavior of
 * Backbone Models & Collections are:
 *
 * - `channels` property (object, merged when extend'ed further) to declare
 *   channel objects to be be defined on the current model/collection/view.
 *   Example:
 *   <pre>
 *     var CueList = Channel.infect(Backbone.Model).extend({
 *       channels: {
 *         cues: 'cues-channel'
 *       },
 *       initialize: function() {
 *         this.cues.command('get-ready');
 *       }
 *     });
 *   </pre>
 *
 * - `actions` property (object, merged when extend'ed, as well) to execute
 *   callbacks on specific events. The key denotes an Event-URL™, the value the
 *   callback to be invoked. Event URLs conform to the following pattern:
 *   Example actions:
 *   <pre>
 *     var Cue = Channel.infect(Backbone.Model).extend({
 *       actions: {
 *         'event://cues/cancel': function() {
 *           this.cancel();
 *         },
 *         'command://cues/get-ready': function() {
 *           this.getReady();
 *         },
 *         'request://cues/save/:id': function() {
 *           return this.toJSON();
 *         }
 *       }
 *     });
 *   </pre>
 *   + The protocol part (e.g. `event://`) denotes the type of listener to
 *     register. Protocol can be either one of `event`, `command` or `request`.
 *     Check the Backbone.Radio documentation for reference.
 *   + The topic part (e.g `..://cues..`), which corresponds to the name of the
 *     channel the listener will be created on.
 *   + The action part (e.g ..://.../create), which corresponds to the event name
 *     being triggered on the channel specified.
 *   + An ID part (optional, e.g. `..://../read/my-wonderful-id`), which can be 
 *     either a literal string, or specified by the `:id` token, which will be
 *     substituted with the current objects ID (if applicable) on construction time.
 * 
 * - `sync` adapter (function, only relevant for models/collections). Uses the
 *   built-in push/pull channels to synchronize model/collection
 *   state.
 *
 * _Channels_ also defines default actions and the `pull` channel on
 * models/collections. For details on those, see below.
 */
var _        = require('underscore');
var Backbone = require('backbone');
var Radio    = require('backbone.radio');
var docuri   = require('docuri');

window.docuri = docuri;
window.res = docuri.route(':type/:topic/:action/(:host_id(/:id))');

/**
 * <pre>
 *  _ __   __ _ _ __ ___  ___ 
 * | '_ \ / _` | '__/ __|/ _ \
 * | |_) | (_| | |  \__ \  __/
 * | .__/ \__,_|_|  |___/\___| function
 * |_|                        
 * </pre>
 * 
 * Take an Event-URI™ (key of `actions` object) and parse it. The structure of
 * the return value is:
 *
 * <pre>
 *   {
 *     type:   '(request|event|command)',
 *     topic:  'Channel Name',
 *     action: '(create|update...|command string)'
 *     id:      'string'
 *   }
 * </pre>
 *
 * There are 2 special fields that will be substituted on parse: 
 * - `:url` will be substituted by the models/collections url field value
 * - `:id` will be substituted by the models `id` value, URL-encoded
 */

docuri.route(':type/:topic/:action(/*path)', 'raw');
docuri.route(':action(/*path)', 'unraw');

var parse = function(uri) {
  var parsed = docuri.raw(uri);
  if(parsed.path) {
    // placeholders are being substituted for their
    // attribute or object values
    var substituted = _.map(parsed.path, function(thing) {
      var attr = thing.slice(1);
      if(this[attr])      return this[attr];
      if(this.attributes) return this.attributes[attr];
                          return null;
    }, this);

    parsed.path = _.map(substituted, function(item) {
      if(typeof item === 'function') {
        return item.apply(this);
      } else {
        return item;
      }
    }, this);
  }
  return parsed;
};

/**
 * <pre>
 *                 _     _            
 *  _ __ ___  __ _(_)___| |_ ___ _ __ 
 * | '__/ _ \/ _` | / __| __/ _ \ '__|
 * | | |  __/ (_| | \__ \ ||  __/ |   
 * |_|  \___|\__, |_|___/\__\___|_| function
 *           |___/                    
 * </pre>
 *
 * Register a supplied `callback` to the `action` on `channel` encoded in `url`.
 */
var register = function(url, callback) {
  var resource = parse.call(this, url);
  var channel  = Radio.channel(resource.topic);
  var parsed   = docuri.unraw(resource);

  switch(resource.type) {
  case 'event':
    channel.on(parsed, callback, this);
    break;
  case 'command':
    channel.comply(parsed, callback, this);
    break;
  case 'request':
    channel.reply(parsed, callback, this);
    break;
  }
};

/**
 * <pre>
 *                             _     _            
 *  _   _ _ __  _ __ ___  __ _(_)___| |_ ___ _ __ 
 * | | | | '_ \| '__/ _ \/ _` | / __| __/ _ \ '__|
 * | |_| | | | | | |  __/ (_| | \__ \ ||  __/ |   
 *  \__,_|_| |_|_|  \___|\__, |_|___/\__\___|_| function
 *                       |___/                    
 * </pre>
 *
 * Unregister a callback for supplied url. 
 */
var unregister = function(url) {
  var resource = parse.call(this, url);
  var channel  = Radio.channel(resource.topic);
  var parsed   = resource.id ? resource.action + '/' + resource.id : resource.action;

  switch(resource.type) {
  case 'event':
    channel.off(parsed, this.actions[url], this);
    break;
  case 'command':
    channel.stopComplying(parsed, this.actions[url], this);
    break;
  case 'request':
    channel.stopReplying(parsed, this.actions[url], this);
    break;
  }
};

/**
 * <pre>
 *           _               
 *  ___  ___| |_ _   _ _ __  
 * / __|/ _ \ __| | | | '_ \ 
 * \__ \  __/ |_| |_| | |_) |
 * |___/\___|\__|\__,_| .__/  function
 *                    |_|    
 * </pre>
 *
 * Setup all `actions` and `channels` on `this` (model/collection/view).
 */
var setupActions = function() {
  if(_.keys(this.actions).length > 0) {
    _.each(this.actions, function(value, key) {
      register.apply(this, [key, value]);
    },this);
  }
};

var setupChannels = function() {
  if(_.keys(this.channels).length > 0) {
    _.each(this.channels, function(value, key) {
      this[key] = Radio.channel(value);
    }, this);
  }
};

var setup = function() {
  setupActions.apply(this);
  setupChannels.apply(this);
};

/**
 * <pre>
 *      _                _    _ _ 
 *  ___| |_ ___  _ __   / \  | | |
 * / __| __/ _ \| '_ \ / _ \ | | |
 * \__ \ || (_) | |_) / ___ \| | |
 * |___/\__\___/| .__/_/   \_\_|_| function
 *              |_|               
 * </pre>
 *
 * Unregister all `actions` and remove channels from `this`.
 */
var stopAll = function() {
  // Delete refs to all channels.
  if(_.keys(this.channels).length >= 0) {
    _.each(this.channels, function(_, key) {
      delete this[key];
    }, this);
  }

  // Unregsiter all `actions`
  if(_.keys(this.actions).length >= 0) {
    _.each(this.actions, function(_, key) {
      unregister.apply(this, [key]);
    },this);
  }
};


var getMeth = function(meth, model) {
  if(model instanceof Backbone.Collection) return 'list';
  return meth;
};
 
/**
 * <pre>                      
 *  ___ _   _ _ __   ___ 
 * / __| | | | '_ \ / __|
 * \__ \ |_| | | | | (__ 
 * |___/\__, |_| |_|\___| function
 *      |___/            
 * </pre>
 *
 * Model/Collection sync adapter. This function basically translates all the
 * different sync methods (CRUD verbs such as `create`, `read`, `update`,
 * `delete`) to calls to the implicitly declared `pull` channel. Clients should
 * implement all 4 verbs in order to provide fully functional sync.
 */
var sync = function(method, model, options) {
  var url = this.url;

  if(typeof this.url === 'function')
    url = this.url.call(this);

  // normalize the push option
  var opts = _.extend({ push: true }, options);

  if(opts.push) {
    // this should not be necessary, but as it stands, its a quick fix for
    // weird bahviors with models and their channels
    if(typeof this.pull === 'undefined')
      this.pull = Radio.channel('pull');

    this.pull.request(getMeth(method, model), url,  model, _.bind(function(err, res) {
      if(err) {
        if(typeof options.error === 'function') {
          options.error.call(this, err);
        } else {
          $.growl("Error during WAMP Request: " + err[0]);
        }
      } else {
        if(typeof options.success === 'function') {
          options.success.call(this, res);
        }
      }
    },this)); 
  } else if(typeof options.success === 'function') {
      options.success.call(this);
  }

  model.trigger('request', model, null, options);
};


/**
 * <pre>
 *   ____ _                            _     
 *  / ___| |__   __ _ _ __  _ __   ___| |___ 
 * | |   | '_ \ / _` | '_ \| '_ \ / _ \ / __|
 * | |___| | | | (_| | | | | | | |  __/ \__ \
 *  \____|_| |_|\__,_|_| |_|_| |_|\___|_|___/ object
 * </pre>
 *
 * Contains the central function for exenting the _Channels_ functionality into
 * other objects and their prototype.
 */
var Channels = {
  infect: function(base, options) {
    // Overridden functions.
    var Base = base.extend({
      constructor: function() {
        base.prototype.constructor.apply(this, arguments);

        if(this.isNew && this.isNew()) {
          this.listenTo(this, 'sync', _.bind(function() {
            setupChannels.apply(this);
            setupActions.apply(this);
          },this));
        } else {
          setup.apply(this);
        }
      },
      stopListening: function() {
        stopAll.apply(this);
        base.prototype.stopListening.apply(this, arguments);
        return this;
      },
      sync: sync
    });

    // Added properties for Model sync.
    if(base === Backbone.Model) {
      Base.prototype.channels = { pull: 'pull' };
      Base.prototype.actions = {
        'event/push/update/:url': function(model) {
          if(model[this.idAttribute] === this.id) {
            this.set(model, { silent: true, push: false });
            this.trigger('iris.updates');
          }
        },
        'event/push/delete/:url': function(model) {
          if(model[this.idAttribute] === this.id) {
            this.destroy();
          }
        }
      };
    }
    // Added Properties for Collection sync.
    else if(base === Backbone.Collection) {
      Base.prototype.channels = { pull: 'pull' };
      Base.prototype.actions = {
        'event/push/create/:url': function(data) {
          if(Array.isArray(data)) {
            data.forEach(function(item) {
              var model = new this.model();
              model.set(model.parse(item));
              this.add(model);
            }, this);
          } else {
            var model = new this.model();
            model.set(model.parse(data));
            this.add(model);
          }
        }
      };
    }

    /**
     * Extending the extension mechanism to allow inheritance of our properties.
     */
    var extend  = base.extend;
    Base.extend = function(protoProps, staticProps) {
      var extended = extend.call(this, protoProps, staticProps);
      if(protoProps && protoProps.actions) {
        _.each(this.prototype.actions, function(cb, action) {
          if(!extended.prototype.actions[action]) {
            extended.prototype.actions[action] = this.prototype.actions[action];
          }
        },this);
      }
      if(protoProps && protoProps.channels) {
        _.each(this.prototype.channels, function(cb, channel) {
          if(!extended.prototype.channels[channel]) {
            extended.prototype.channels[channel] = this.prototype.channels[channel];
          }
        },this);
      }
      return extended;
    };

    return Base;
  }
};

module.exports = Channels;
