/**
 * Abstraction for a Transport socket to an **Iris** instance
 *
 * This class strives to abstract away from specific libraries underneath and
 * provide a unified interface for client-side code. Its also meant to hide some
 * of uglyness associated with the various socket libraries out in the wild.
 *
 * For now, we use the **autobahn** WAMP library.
 */
var _        = require('underscore');
var autobahn = require('autobahn');

/**
 * @constructor
 * @param {string} host  - The host to connect to, eg: '192.168.23.15' or 'iris02.local'.
 * @param {number} port  - The port to connect on, eg: 3333.
 * @param {string} realm - The the WAMP Realm, e.g. "realm1" (default).
 */
var Transport = function(host, port, realm) {
  this.subscriptions = {};
  this.host          = host;
  this.port          = port;
  this.realm         = realm || "realm1";
  this._init();
};


/**
 * ___init__ (private)
 * Private initializer for the connection, so we can reuse this code when
 * manually re-connecting to the server.
 */
Transport.prototype._init = function() {
  this.connection = new autobahn.Connection({
    url: 'ws://' + this.host + ':' + this.port,
    realm: this.realm || 'realm1'
  });

  // When successfully connected to a server, execute the callback provided as
  // argument to the `open(cb)` method call, but save the returned session object as
  // field to this Transport instance first.
  // The callback function signature follows the *node.js*-style convention.
  this.connection.onopen = _.bind(function(session) {
    this.session = session;
    if(typeof this.onconnect === 'function') {
      this.onconnect(null, session);
    }

    console.log("Connected to ", this.host, " on ", this.port,
                ". Activating topic subscriptions.");

    // Now we activate all topic channel subscriptions that we have registered
    // in our global subscriptions object.
    _.map(_.keys(this.subscriptions), function(topic) {
      this._activate(topic);
    },this);
  }, this);

  // Callback to exectute when the connection was either lost, closed or the
  // host was unreachable. When called, the stored session object is deleted,
  // and the callback provided during `open(cb)` is called with object
  // containing diagnostic information.
  // The callback function signature follows the *node.js*-style convention.
  this.connection.onclose = _.bind(function(reason, details) {
    if(typeof this.onconnect === 'function')
      this.onconnect({ reason: reason, details: details}, null);

    delete this.session;
  },this);
};


/**
 * __Connect to host__
 *
 * Open a connection to the _Iris_ server application, optionally registering a
 * callback to be invoked upon successful/failed connection. The function
 * signature of the supplied callback adheres to _node.js_ convention, e.b. :
 * `cb(err, session)`. As indicated, the second argument (passed on successful
 * connection) is a `session` object (for now, the raw autobahn session object).
 * Use that to subscribe to new broadcast channels etc.
 *
 * @param {function} callback - Callback to invoke once the connection was
 *                              established (this adheres to node-style callback
 *                              signatures, e.g. cb(err, data))
 */
Transport.prototype.connect = function(cb) {
  if(typeof cb === 'function') {
    this.onconnect = cb;
  }
  if(this.connection) {
    this.connection.open();
  } else {
    this._init();
    this.connection.open();
  }
  return this;
};


/**
 * __Disconnect from host__ and clean up all internal state associated with the
 * previous connection.
 */
Transport.prototype.disconnect = function() {
  if(this.connection && this.session) {
    this.connection.close();
    delete this.session;
    delete this.connection;
  }
  return this;
};


/**
 * __Reconnect to host__ (powercycle the connection).
 */
Transport.prototype.reconnect = function() {
  this.disconnect().connect(this.onconnect);
  return this;
};


/**
 * __Check Transport status__
 */
Transport.prototype.connected = function() {
  return typeof this.session === 'object';
};


/**
 * __Call a remote procedure__
 * @param {string} action - an Iris API endpoint, e.g. "iris.cues"
 * @param {array} args    - an array of arguments to pass to the RPC
 * @param {function} cb   - a Callback to invoke when done (called with a
 *                          node-style signature: cb(err, data))
 */
Transport.prototype.call = function(action, args, cb) {
  if(typeof this.session === 'undefined')
    throw "Not connected!";

  try {
    var data = [this.session._id].concat(args);
    this.session.call(action, data).then(function(response) {
      cb(null, response);
    }, function(err) {
      cb(err, null);
    });
  } catch(e) {
    cb(e, null);
  }
};

/**
 * __Subscribe to remote changes feeds__
 * @param {string} action - an Iris Feed endpoint, e.g. "iris.updates"
 * @param {function} cb - a Callback to invoke when a message was received
 *                          (called with a node-style signature: cb(err, data))
 */
Transport.prototype.subscribe = function(topic, cb) {
  // Unless the subscription already exists, add it to the global dictionary.
  if(typeof this.subscriptions[topic] === 'undefined') {
    this.subscriptions[topic] = {
      active: false,
      topic: topic,
      callback: cb
    };
  }
  this._activate(topic);
};

/**
 * __Activate all topic subscriptions__
 *
 * The requested subscription will be activated, unless of course Transport is
 * not connected.
 *
 * @param {string} topic - The topic to activate.
 */
Transport.prototype._activate = function(topic) {
  if(typeof this.session === 'undefined') {
    return;
  }

  if(!this.subscriptions[topic].active) {
    // __Subscription Success Callback__
    //
    // The subscription was successful, so we save a reference to the
    // subscription in our global data structure for convenience.
    var success = _.bind(function(sub) {
      this.subscriptions[topic].active = true;
      this.subscriptions[topic].subscription = sub;
    },this);

    // __Subscription Error Callback__
    //
    // The subscription was not successful, so we notify the caller that it
    // crapped out and set the active state to false.
    var error = _.bind(function(err) {
      this.subscriptions[topic].active = false;
      this.subscriptions[topic].callback(err, null);
    },this);

    this.session
      .subscribe(topic, _.bind(function(data) {
        // This message is from for me. No processing..
        if(parseInt(data[0],10) === this.session._id) return;
        // Now, go for it.
        try {
          this.subscriptions[topic].callback(null, data);
        } catch(e) {
          $.growl("Transport: subscription error " + e);
        }
      },this))
      .then(success, error);
  }
};

/**
 * __Deactivate topic subscription__
 *
 * Unregister the requested topic subscription from session (if connected).
 *
 * @param {string} topic - The topic to deactivate.
 */
Transport.prototype._deactivate = function(topic) {
  if(typeof this.session === 'undefined') {
    $.growl("Transport: deactivation not possible.<br>Already disconnected.",{
      type: 'danger'
    });
    return;
  }

  if(this.subscriptions[topic].active) {
    // __Unsubscription Success Callback__
    //
    // The unsubscription was successful, so we delete the reference to the
    // subscription in our global data structure and set `active` to `false`.
    var success = _.bind(function(sub) {
      this.subscriptions[topic].active = false;
      delete this.subscriptions[topic].subscription;
    },this);

    // __Unsubscription Error Callback__
    //
    // The unsubscription was not successful, so we notify the caller that it
    // crapped out.
    var error = _.bind(function(err) {
      this.subscriptions[topic].active = false;
      this.subscriptions[topic].callback(err, null);
    },this);

    this.session
      .unsubscribe(this.subscriptions[topic].subscriptions)
      .then(success, error);
  }
};

module.exports = Transport;
