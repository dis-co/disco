var Base = require('../base');

var NetworkView = Base.ItemView.extend({
  template: require('./templates/network.hbs'),

  templateHelpers: function() {
    return {
      Network: this.model.get("Network")
    };
  },

  initialize: function(options) {
    this.model = this.collection.last();
    this.listenTo(this.collection, 'add', function(model) {
      this.model = model;
      this.render();
    });
  }
});

module.exports = NetworkView;
