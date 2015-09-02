var _     = require('underscore');
var Base  = require('../components/base');
var Cue   = require('../components/cue');
var Hosts = require('./hosts.js');
var Radio = require('backbone.radio');
var Chans = require('../../lib/channels.js');

var Toolbar = Chans.infect(Base.LayoutView).extend({
  template: require('./templates/toolbar.hbs'),

  className: 'toolbar',

  editing: false,

  events: {
    'click button.reload': 'reload',
    'click button.online':  'offline',
    'click button.offline': 'online',
    'click button.create':  'create',
    'click button.update':  'save',
    'click button.save':    'save',
    'click button.cancel':  'cancel'
  },

  ui: {
    online:  'button.online',
    offline: 'button.offline',
    create:  'button.create',
    save:    'div.save-group',
    update:  'div.update-group'
  },

  regions: {
    'hosts': '.hostlist'
  },

  actions: {
    'event/keyboard/keyup': function (ev) {
      if(ev.keyCode == 27) this.cancel();
    },
    'event/keyboard/keydown': function (ev) {
      if(ev.ctrlKey && ev.keyCode == 83 && this.editing)
        this.save();
    }
  },

  initialize: function(options) {
    this.cues = options.cues;
    this.listenTo(this.cues, 'edit', this.edit);
    this.listenTo(this.collection, 'reset', function() {
      setTimeout(_.bind(function() {
        this.collection.fetch();
      },this), 200);
    });
  },

  onRender: function() {
    this.online();
    this.hosts.show(new Hosts({
      patches: this.collection
    }));
  },

  reload: function(ev) {
    ev.preventDefault();
    this.collection.reset();
  },
  
  showCreate: function() {
    this.ui.save.addClass('hidden');
    this.ui.update.addClass('hidden');
    this.ui.create.removeClass('hidden');
    this.ui.online.show();
    this.ui.offline.show();
  },

  showSave: function() {
    this.ui.create.addClass('hidden');
    this.ui.save.removeClass('hidden');
    this.ui.online.hide();
    this.ui.offline.hide();
  },

  showUpdate: function() {
    this.ui.create.addClass('hidden');
    this.ui.update.removeClass('hidden');
    this.ui.online.hide();
    this.ui.offline.hide();
  },

  online: function(event) {
    this.ui.online.removeClass('hidden');
    this.ui.offline.addClass('hidden');
    Radio.channel('pins').trigger('live', true);
  },

  offline: function(event) {
    this.ui.online.addClass('hidden');
    this.ui.offline.removeClass('hidden');
    Radio.channel('pins').trigger('live', false);
  },

  edit: function (ev) {
    if(ev) ev.preventDefault();
    this.showUpdate(); 
    this.editing = true;
  },

  create: function(event) {
    if(event) event.preventDefault();
    this.cues.new();
    this.showSave();
    this.editing = true;
  },

  save: function(event) {
    if(event) event.preventDefault();
    this.cues.save();
    this.showCreate();
    this.editing = false;
  },

  cancel: function(event) {
    if(event) event.preventDefault();
    this.cues.cancel();
    this.showCreate();
    this.editing = false;
  }
});

module.exports = Toolbar;
