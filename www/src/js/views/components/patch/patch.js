var Base   = require('../base');
var Pin    = require('../pin');
var models = require('../../../models');

var Patch = Base.CompositeView.extend({
  className: 'panel panel-default',

  template: require('./templates/patch.hbs'),

  templateHelpers: function () {
    return {
      DebugString: this.model.get('FilePath') === 'debug-string' ? "debug-string" : ""
    }
  },

  childViewContainer: '#pin-list',

  childEvents: {
    select: function() {
      this.select();
    },
    cancel: function() {
      this.cancel();
    }
  },

  events: {
    'click button.visibility': 'toggle'
  },

  ui: {
    header:     '.panel-heading',
    body:       '.panel-body',
    visibility: 'button.visibility'
  },

  initialize: function(options) {
    this.ids = options.ids;
    this.isVisible = true;
  },

  toggle: function(event) {
    if(this.isVisible) {
      this.hide();
    } else {
      this.show();
    }
  },

  select: function() {
    this.$el.removeClass('panel-default');
    this.$el.addClass('panel-danger');
  },

  cancel: function() {
    this.$el.addClass('panel-default');
    this.$el.removeClass('panel-danger');
  },

  show: function() {
    this.ui.body.show();
    this.ui.visibility.children('i.down').removeClass('down');
    this.isVisible = true;
  },

  hide: function() {
    this.ui.body.hide();
    this.ui.visibility.children('i').addClass('down');
    this.isVisible = false;
  },

  getChildView: function(model) {
    return Pin[model.type()][model.behavior()];
  },

  filter: function (child, index, collection) {
    if(this.ids && this.ids.length > 0) {
      if(this.ids.indexOf(child.id) === -1) {
        return false;
      }
      return true
    } else {
      return true;
    }
  }
});

module.exports = Patch;
