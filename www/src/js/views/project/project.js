var Base    = require('../components/base');
var Confirm = require('./confirm.js');

module.exports = Base.ItemView.extend({
  template: require('./templates/project.hbs'),

  className: 'project row',

  events: {
    'dblclick': 'load',
    'click button.load': 'load',
    'click button.delete': 'delete'
  },

  initialize: function(options) {
    this.detailed = options.detailed;
    this.listenTo(this.model, 'change', this.render);
  },

  load: function() {
    this.focus();
    this.trigger('load', this.model.id);
  },

  focus: function() {
    this.$el.addClass('active');
  },

  unfocus: function() {
    this.$el.removeClass('active');
  },
  
  delete: function() {
    new Confirm({
      model: this.model
    }).show();
  },

  templateHelpers: function() {
    return {
      detailed: this.detailed || false,
      Project:  this.model.attributes
    };
  }
});
