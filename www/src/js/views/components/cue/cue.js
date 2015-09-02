var _       = require('underscore');
var Base    = require('../base');
var Channel = require('../../../lib/channels.js');
var Editor  = require('./edit.js');

var Cue = Channel.infect(Base.ItemView).extend({
  tagName:   'li',
  className: 'item cue',
  template:  require('./templates/list-item.hbs'),
  draggable: false,

  events: {
    'click button.abort'       : 'abort',
    'click button.delete-cue'  : 'requestDelete',
    'click button.destroy-cue' : 'delete',
    'click button.play-cue'    : 'play',
    'click button.update-name' : 'updateName',
    'click button.edit-values' : 'editValues',
    'click button.edit-cue'    : 'editCue'
  },

  ui: {
    tooltips:     '[data-toggle="tooltip"]',
    trash:        'button.delete-cue',
    edit:         'button.edit-cue',
    play:         'button.play-cue',
    deleteDialog: 'a.delete-dialog'
  },
 
  actions: {
    'event/cuelists/edit': function() {
      this.draggable = true;
      this.render();
    },
    'event/cuelists/cancel': function() {
      this.draggable = false;
      this.render();
    },
    'event/keyboard/keyup': function (ev) {
      if(ev.keyCode == 27) this.abort();
    }
  },

  channels: {
    cuelists: 'cuelists'
  },

  enabled: true,
  
  initialize: function(options) {
    this.editable  = options.editable  || false;
    this.trashable = options.trashable || false;

    this.listenTo(this.model, 'sync',   this.render);
    this.listenTo(this.model, 'cancel', this.enable);

    this.listenTo(this.model, 'iris.updates', function() {
      this.trigger('unfocus');
      this.render();
    });

    this.listenTo(this, 'before:destroy', function() {
      // important! model gets torn-down when view is released, unregistering
      // all cbs and stuff! be careful removing this!
      this.model = null;
    });
  },

  onRender: function() { 
    if(!this.editable)
      this.ui.edit.hide();

    if(!this.trashable)
      this.ui.trash.hide();
    
    this.ui.tooltips.tooltip();
    this.ui.deleteDialog.hide();

    if(this.draggable) {
      if(this.draggableInitialized) {
        this.$el.draggable('enable');
      } else {
        this.makeDraggable();
      }
    } else {
      if(this.draggableInitialized) {
        this.$el.draggable('disable');
      }
    }
  },

  makeDraggable: function() {
    this.$el.draggable({
      cursor: 'move',
      helper: _.bind(function() {
        var $el = $('<li class="cue-drag"></li>');
        $el.html(this.model.get('Name'));
        $el.attr('id', this.model.id);
        $el.css('width', $('.cue-list-region ul').width());
        return $el;
      },this),
      connectToSortable: '.cue-list-region ul'
    });
    this.draggableInitialized = true;
  },

  editCue: function() {
    this.abort();
    new Editor({ model: this.model }).show();
  },

  editValues: function() {
    this.model.collection.trigger('edit');
    this.model.edit();
    this.activate();
  },
  
  requestDelete: function(event) {
    this.abort();
    this.ui.trash.attr('disabled', 'disabled');
    this.ui.deleteDialog.show();
  },

  load: function() {
    if(this.enabled) {
      this.trigger('unfocus');
      this.model.activate();
      this.activate();
    }
  },

  abort: function(event) {
    this.ui.trash.attr('disabled', null);
    this.ui.deleteDialog.hide();
  },

  delete: function() {
    this.model.destroy();
  },

  play: function() {
    this.trigger('unfocus');
    this.model.play();
    this.load();
    this.activate();
  },

  activate: function() {
    this.$el.addClass('active');
  },

  unfocus: function() {
    this.$el.removeClass('active');
  },

  disable: function() {
    this.enabled = false;
    this.ui.edit.attr('disabled',  'disabled');
    this.ui.trash.attr('disabled', 'disabled');
    this.ui.play.attr('disabled',  'disabled');
  },

  enable: function() {
    this.enabled = true;
    this.ui.edit.attr('disabled',  null);
    this.ui.trash.attr('disabled', null);
    this.ui.play.attr('disabled',  null);
  },

  updateName: function() {
    return;
    this.model.save({ Name: this.ui.nameInput.val() });
  }
});

module.exports = Cue;
