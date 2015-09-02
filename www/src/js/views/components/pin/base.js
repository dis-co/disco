var Base  = require('../base');
var Radio = require('backbone.radio');

var Slice = Base.ItemView.extend();

var Pin = Base.CompositeView.extend({
  template: require('./templates/base.hbs'),
  className: 'pin row',

  childViewContainer: '.slices',

  events: {
    'change .cue-toggle': 'toggleSelected'
  },

  ui: {
    name:   '.title',
    toggle: '.cue-toggle',
    inlay:  '.inlay'
  },

  templateHelpers: function() {
    return {
      Pin: this.model.attributes
    };
  },

  // FIXME: should not override constructor
  constructor: function() {
    Base.CompositeView.prototype.constructor.apply(this, arguments);
    this.listenTo(this.model, 'iris.updates', this.update);
    this.listenTo(this.model, 'selectable',   this.selectable);
    this.listenTo(this.model, 'cancel',       this.cancel);
    this.listenTo(this.model, 'select',       this.select);
  },

  update: function() {
    this.ui.name.html(this.model.get('Name'));
  },

  toggleSelected: function(event, args) {
    if(!this.model.selectable()) return;
    this.model.selected(this.ui.toggle.prop('checked'));
    this.setInlay();
  },

  selectable: function() {
    if(this.model.selectable()) {
      this.ui.toggle.bootstrapToggle('enable');
      this.ui.toggle.bootstrapToggle(this.model.selected() ? 'on' : 'off');
    } else {
      this.ui.toggle.bootstrapToggle('off');
      this.ui.toggle.bootstrapToggle('disable');
    }
    this.setInlay();
  },

  select: function() {
    this.ui.toggle
      .bootstrapToggle(this.model.selected() ? 'on' : 'off');
    this.setInlay();
    this.trigger('select');
  },

  cancel: function() {
    this.selectable();
    this.trigger('cancel');
  },

  setInlay: function() {
    if(this.model.selectable()) {
      if(this.model.selected()) {
        this.ui.inlay.addClass('bg-danger');
      } else {
        this.ui.inlay.removeClass('bg-danger');
      }
    } else {
        this.ui.inlay.removeClass('bg-danger');
    }
  },
  
  onShow: function() {
    this.ui.toggle.bootstrapToggle({
      on:  'Cue',
      off: ''
    });
  }
});

module.exports.Pin   = Pin;
module.exports.Slice = Slice;
