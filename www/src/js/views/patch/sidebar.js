var _        = require('underscore');
var Base     = require('../components/base');
var Cue      = require('../components/cue');
var Radio    = require('backbone.radio');
var Channels = require('../../lib/channels.js');

var SideBar = Channels.infect(Base.LayoutView).extend({
  template: require('./templates/sidebar.hbs'),

  regions: {
    cues: '.cueslist'
  },

  actions: {
    'event/keyboard/keyup': function(ev) {
      if(ev.keyCode == 27) // ESC
        this.cancel();
    }
  },

  onRender: function() {
    var cuelist = new Cue.GroupedList({
      collection: this.collection.getTags(),
      cues: this.collection,
      editable: true,
      trashable: true
    });
    this.cues.show(cuelist);
  },

  cancel: function() {
    this.collection.cancel();
  }
});

module.exports = SideBar;
