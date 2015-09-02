var Base    = require('../base');
var Channel = require('../../../lib/channels.js');

var Cuelist = Channel.infect(Base.ItemView).extend({
  tagName:   'li',
  className: 'item',
  template:   require('./templates/list-item.hbs'),

  events: {
    'click'                    : 'select',
    'click button.abort'       : 'abort',
    'click button.edit-cue'    : 'editName',
    'click button.update-name' : 'updateName',
    'click button.delete-cue'  : 'requestDelete',
    'click button.destroy-cue' : 'delete'
  },

  ui: {
    tooltips:     '[data-toggle="tooltip"]',
    trash:        'button.delete-cue',
    editDialog:   'a.edit-dialog',
    deleteDialog: 'a.delete-dialog',
    nameInput:    'a.edit-dialog input'
  },

  channels: {
    cuelists: 'cuelists'
  },

  initialize: function() {
    this.listenTo(this.model, 'sync', this.render);
    this.listenTo(this.model, 'iris.updates', this.render);
  },

  onRender: function() {
    this.ui.tooltips.tooltip();
    this.ui.editDialog.hide();
    this.ui.deleteDialog.hide();
  },

  updateName: function() {
    this.model.save({ Name: this.ui.nameInput.val() });
  },
  
  editName: function() {
    this.abort();
    this.ui.editDialog.show();
    this.ui.nameInput.focus();
  },

  select: function(event) {
    this.selected = true;
    if(this.cuelists) this.cuelists.trigger('edit');
    this.trigger('select');
    this.activate();
  },

  deselect: function(event) {
    this.selected = false;
    if(this.cuelists) this.cuelists.trigger('cancel');
    this.deactivate();
  },

  activate: function() {
    this.selected = true;
    this.$el.addClass('active');
  },

  deactivate: function() {
    this.selected = false;
    this.$el.removeClass('active');
  },

  abort: function() {
    this.ui.trash.attr('disabled',null);
    this.ui.nameInput.val('');
    this.ui.editDialog.hide();
    this.ui.deleteDialog.hide();
  },
  
  requestDelete: function(event) {
    this.abort();
    this.ui.trash.attr('disabled', 'disabled');
    this.ui.deleteDialog.show();
  },

  delete: function() {
    if(this.selected) this.deselect();
    this.model.destroy();
    this.destroy();
  }
});

module.exports = Cuelist;
