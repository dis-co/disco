var _    = require('underscore');
var Base = require('../base');
var Channels = require('../../../lib/channels.js');

var Editor = Channels.infect(Base.ItemView).extend({
  template: require('./templates/edit.hbs'),

  className: 'modal fade',

  events: {
    'click button.save': 'save',
    'click button.dismiss': 'close',
    'hide.bs.modal': 'closed'
  },

  ui: {
    "name":  "input[name=name]",
    "tags":  "input[name=tags]",
    "hosts": "input[name=hosts]"
  },

  actions: {
    'event/keyboard/keyup': function(ev) {
      if(ev.keyCode == 27)
        this.close();
    },
    'event/keyboard/keydown': function (ev) {
      if(ev.ctrlKey && ev.keyCode == 83)
        this.save();
    }
  },

  save: function() {
    this.model.save({
      Tags:  this.ui.tags.select2('val'),
      Hosts: this.ui.hosts.select2('val'),
      Name:  this.ui.name.val()
    },{
      success: _.bind(function() {
        this.close();
      },this)
    });
    this.model = null; // important, otherwise model gets destroyed as well!
  },

  onRender: function() {
    this.ui.tags.select2({
      tags: this.model.get("Tags")
    });
    this.ui.hosts.select2({
      tags: this.model.get("Hosts")
    });
  },

  closed: function() {
    this.destroy();
  },

  close: function() {
    this.ui.tags.select2('destroy');
    this.ui.hosts.select2('destroy');
    this.model = null; // important, otherwise model gets destroyed as well!
    $('#modals').children().first().modal('hide');
  },

  show: function() {
    this.render();
    $('#modals').html(this.$el);
    $('#modals').children().first().modal('show');
  }
});

module.exports = Editor;
