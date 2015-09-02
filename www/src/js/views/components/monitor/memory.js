var Base = require('../base');

var MemoryView = Base.ItemView.extend({
  template: require('./templates/memory.hbs'),

  templateHelpers: function() {
    return {
      allocated: this.model.get("Memory")["Allocated Objects"],
      total: this.model.get("Memory")["Total Physical Memory"]
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

module.exports = MemoryView;
