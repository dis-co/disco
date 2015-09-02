var Backbone = require('backbone');
var _        = require('underscore');

var Settings = require('../../app/settings.js');
var Base     = require('../components/base');
var Cue      = require('../components/cue');

var SETTING = 'hostlist-selection';

var Host = Backbone.Model.extend({
  url: 'iris.hosts',

  idAttribute: 'HostName'
});

var Hosts = Backbone.Collection.extend({
  url: 'iris.hosts',
  comparator: 'HostName',
  model: Host
});

var HostView = Base.ItemView.extend({
  'tagName': 'option',

  template: false,
  
  onRender: function() {
    var hostname = this.model.get('HostName');
    this.$el.attr('value', hostname);
    this.$el.html(hostname);

    if(Settings.get(SETTING) === hostname)
      this.$el.attr('selected', 'selected');
  }
});

var HostList = Base.CollectionView.extend({
  tagName: 'select',

  className: 'form-control',

  childView: HostView,

  events: {
    'change': 'select'
  },

  initialize: function(options) {
    this.collection = this.collection || new Hosts();
    this.patches = options.patches;
    this.listenTo(this.patches, 'add update', this.syncHosts);

    var last = Settings.get(SETTING);
    if(last === null) Settings.save(SETTING, 'All');
  },

  syncHosts: function(ev) {
    this.patches.each(function(item, idx) {
      this.collection.add(new Host({
        HostName: item.get('HostName')
      }));
    }, this);
  },

  onRender: function() {
    var view = new HostView({
      model: new Host({
        HostName: "All"
      })
    });
    this.$el.prepend(view.render().el);
  },

  select: function(ev) {
    var host = this.$el.find('option:selected').first().text();
    this.patches.setHost(host);
    Settings.save(SETTING, host);
  }
});

module.exports = HostList;
