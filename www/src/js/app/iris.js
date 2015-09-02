var docuri     = require('docuri');
var Backbone   = require('backbone');
var Radio      = require('backbone.radio');
var Marionette = require('backbone.marionette');

var _         = require('underscore');
var views     = require('../views');
var models    = require('../models');
var Router    = require('./router.js');
var Transport = require('../lib/transport.js');
var Settings  = require('../app/settings.js');

var DEBUG = Settings.get("debug") || false;

/**
 * __Iris Application__
 */
var Iris = new Marionette.Application();

/**
 * __Adding all application-level regions__.
 *
 */
Iris.addRegions({
  overlay: '#blur-overlay',
  nav:     '#navbar',
  main:    '#main'
});

Iris.addInitializer(function() {
  $(window).on('blur', function() {
    var view = new views.Components.Blur();
    Iris.overlay.show(view);
  });

  $(window).on('focus', function() {
    Iris.overlay.empty();
  });

  var keychan = Radio.channel('keyboard');

  $(document).keydown(function(ev) {
    if(ev.ctrlKey && ev.keyCode == 83)
      ev.preventDefault();

    keychan.trigger('keydown', ev.originalEvent);
  });

  $(document).keyup(function(ev) {
    keychan.trigger('keyup', ev.originalEvent);
  });

  $(document).keypress(function(ev) {
    keychan.trigger('keypress', ev.originalEvent);
  });
});

/**
 * __Initialize the Navigation Bar__
 */
Iris.addInitializer(function() {
  
});

/**
 * __Initialize Transport and Routing__
 */
Iris.addInitializer(function(options) {
  var live = true;
  var transport = new Transport(window.location.hostname, 9500, 'realm1');

  /**
   * Iris defines 3 main channels for hooking up models & collections to WAMP
   * transport.
   *
   * - `pull` implements traditional request-based information flow similar to XHR
   * - `push` is an server-to-client channel for asynchronous updates from other clients
   * - `transport` delivers WAMP status events to interested parties
   */
  window.Radio = Radio;

  var pins   = Radio.channel('pins');
  var pull   = Radio.channel('pull');
  var push   = Radio.channel('push');
  var app    = Radio.channel('app');
  var status = Radio.channel('transport');

  var radios = [
    'pull',
    'push',
    'pins',
    'cue',
    'cues',
    'cuelists',
    'transport'
  ];

  app.reply('debug/get', function () {
    return DEBUG;
  });

  app.on('debug/set', function (debug) {
    DEBUG = debug;
    if(DEBUG) {
      radios.forEach(Radio.tuneIn);
      Backbone.Radio.DEBUG = true;
      window._ = _;
      window.Settings = Settings;
    } else {
      radios.forEach(Radio.tuneOut);
      Backbone.Radio.DEBUG = false;
      window._             = null;
      window.Settings      = null;
    }
  });

  // set debug mode
  app.trigger('debug/set', DEBUG);

  /**
   * <pre>
   *  ____            _
   * |  _ \ _   _ ___| |__
   * | |_) | | | / __| '_ \
   * |  __/| |_| \__ \ | | |
   * |_|    \__,_|___/_| |_| channel
   * </pre>
   *
   * Wiring up the `push` channel to the WAMP pub/sub topic 'iris.updates'.
   *
   * - `data[0]` - current session id (not of interest to our models)
   * - `data[1]` - request URI (e.g. `iris.cue/update/2342-234fa2-234323`)
   * - `data[2]` - payload (optional, the object/model in question)
   */
  docuri.route(':action/:resource', 'unparse');
  docuri.route(':resource/:action', 'parse');

  transport.subscribe('iris.updates', function(err, data) {
    if(!live) return;

    var uri = docuri.unparse(docuri.parse(data[1]));
    var payload;

    try {
      if(typeof data[2] === 'string') {
        try {
          payload = $.parseJSON(data[2]);
        } catch(e) {
          payload = data[2];
        }
      } else {
        payload = data[2];
      }
      push.trigger(uri, payload);
    } catch(e) {
      $.growl("Transport error on iris.updates:<br/>" + e , { type: 'danger' });
    }
  });

  pins.on('live', function(val) {
    live = val;
  });

  /**
   * <pre>
   *  ____        _ _
   * |  _ \ _   _| | |
   * | |_) | | | | | |
   * |  __/| |_| | | |
   * |_|    \__,_|_|_| channel
   * </pre>
   *
   * `create` a resource on the server, executing callback (err, result) on completion
   */
  pull.reply('create', function(url, model, callback) {
    transport.call(url + '/create', [model.attributes], function(err, res) {
      callback(err, res);
    });
  });

  /**
   * `read` a resource from the server. ID is passed along, if a model is passed
   * in. (Collections don't pass any model data).
   */
  pull.reply('read', function(url, model, callback) {
    transport.call(url + '/read', [model.id], callback);
  });

  /**
   * `list` a colleciton from the server.
   */
  pull.reply('list', function(url, model, callback) {
    transport.call(url + '/list', [], callback);
  });

  /**
   * `update` a resource (model) by ID on the server.
   */
  pull.reply('update', function(url, model, callback) {
    transport.call(url + '/update', [model.attributes], callback);
  });

  /**
   * `delete` a resource (model) by ID on the server.
   */
  pull.reply('delete', function(url, model, callback) {
    transport.call(url + '/delete', [model.attributes], callback);
  });

  /**
   * Connect to specified server and emit `connected` event on 'transport'
   * channel.
   */
  transport.connect(function(err, session) {
    if(err) {
      $.growl('Transport initialization error: ' + err, {
        type: 'danger'
      });

      status.trigger('disconnected');
      return;
    }
    status.trigger('connected');

    var projects = new models.Project.Collection();
    projects.fetch({
      reset: true,
      success: function (arg) {
        Iris.router = new Router(projects);
        Iris.nav.show(new views.Navigation({
          projects: projects,
          router: Iris.router
        }));
        Backbone.history.start();
      }
    });
  });

  this.transport = transport;
});

module.exports = Iris;
