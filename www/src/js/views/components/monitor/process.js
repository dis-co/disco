var Base = require('../base');

var ProcessView = Base.ItemView.extend({
  template: require('./templates/process.hbs'),

  templateHelpers: function() {
    return {
      Processes: this.model.get("Processes")
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

module.exports = ProcessView; 
