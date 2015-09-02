var _        = require('underscore');
var Base     = require('../components/base');
var Radio    = require('backbone.radio');
var Backbone = require('backbone');
var Channels = require('../../lib/channels.js');

var Cue = Base.ItemView.extend({
  template:  require('./templates/cue.hbs'),

  className: 'col-xs-12',

  templateHelpers: function() {
    return {
      Name: this.model.get('cue').get('Name')
    };
  },

  ui: {
    cue: '.cue',
    icon: 'i.fa'
  },

  events: {
    'dblclick':        'play',
    'click .play-cue': 'play'
  },

  initialize: function () {
    this.listenTo(this.model, 'play', this.play);
    this.listenTo(this.model, 'change:current', this.setFocus);
  },

  play: function() {
    this.model.set({ current: true });
    this.model.get('cue').play();
    this.trigger('play');
  },

  center: function (arg) {
    var pos = (this.$el.position().top -
                this.$el.parent().position().top) - this.$el.height();
    $("#main").animate({ scrollTop: pos }, 200);
  },

  setFocus: function() {
    if(this.model.get('current')) {
      this.center();
      this.ui.cue.addClass('active');
      this.ui.icon.removeClass('fa-play');
      this.ui.icon.addClass('fa-pause');
    } else {
      this.ui.cue.removeClass('active');
      this.ui.icon.removeClass('fa-pause');
      this.ui.icon.addClass('fa-play');
    }
  }
});

var CuesList = Base.CollectionView.extend({
  tagName:   'div',
  className: 'row play-list',
  childView:  Cue,

  childEvents: {
    'play': function (current) {
      this.collection.each(function (model) {
        if(model.get('current') &&
           model.get("index")   != current.model.get('index'))
        {
          model.set({ current: false });
        }
      });
    }
  }
});

module.exports = Channels.infect(Base.LayoutView).extend({
  template: require('./templates/cues.hbs'),

  regions: {
    main: '.cues'
  },

  actions: {
    'event/keyboard/keydown': function(ev) {
      this.processKeys(ev);
    }
  },

  initialize: function(options) {
    this.tmp = new Backbone.Collection();
    this.cues = options.cues;
    this.cuelists = options.cuelists;
    this.listenTo(this.cuelists, 'load', this.load);
  },

  processKeys: function(event) {
    switch(event.keyCode) {
    case 13: // right
      this.nextCue();
      break;
    case 39: // right
      this.nextCue();
      break;
    case 37: // left
      this.prevCue();
      break;
    case 38: // up
      this.prevCue();
      break;
    case 40: // down
      this.nextCue();
      break;
    case 32: // space
      this.nextCue();
      break;
    case 36: // home
      this.playFirst();
      break;
    case 35: // end
      this.playLast();
      break;
    }
  },

  nextCue: function() {
    var playing = this.tmp.where({ current: true });

    if(playing.length == 0) {
      var next = this.tmp.first();
      if(next) next.trigger('play');
    } else {
      var lastidx = _.last(playing).get('index');
      var next = this.tmp.findWhere({ index: lastidx + 1 });
      if(next) {
        _.each(playing, function(model) {
          model.set({ current: false });
        });
        next.trigger('play');
      }
    }
  },

  prevCue: function() {
    var playing = this.tmp.where({ current: true });
    if(playing.length > 0) {
      var lastidx = _.last(playing).get('index');
      var prev = this.tmp.findWhere({ index: lastidx - 1 });
      if(prev) {
        _.each(playing, function(model) {
          model.set({ current: false });
        });
        prev.trigger('play');
      }
    }
  },

  playFirst: function() {
    this.tmp.each(function(model) {
      model.set({ current: false });
    });
    var first = this.tmp.first();
    if(first) first.trigger('play');
  },

  playLast: function() {
    this.tmp.each(function(model) {
      model.set({ current: false });
    });
    var last = this.tmp.last();
    if(last) last.trigger('play');
  },

  load: function(id) {
    var ids = this.cuelists.get(id).get("Cues");

    this.tmp.reset();
    this.tmp = _.reduce(ids, function(coll, id, idx) {
      coll.add({
        index: idx,
        current: false,
        cue: this.cues.get(id)
      });
      return coll;
    }, this.tmp, this);
    
    this.main.empty();
    this.main.show(new CuesList({
      collection: this.tmp
    }));
  }
});
