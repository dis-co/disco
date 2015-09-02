var Base  = require('../components/base');
var Radio = require('backbone.radio');

var Item = Base.ItemView.extend({
  template: false,
  tagName: 'option',

  onRender: function() {
    this.$el.attr('value', this.model.id);
    this.$el.html(this.model.get('Name'));
  }
});

module.exports = Base.CompositeView.extend({
  template: require('./templates/topbar.hbs'),
  className: 'row bar',
  childView: Item,
  childViewContainer: 'select.cuelists',

  ui: {
    select: 'select.cuelists'
  },

  events: {
    'change select.cuelists': 'select'
  },

  onShow: function() {
    this.ui.select
      .prepend('<option value="" disabled selected>Select Cuelist</option>');
  },

  select: function(event) {
    this.collection.trigger('load', $(event.target).val());
  }
});
