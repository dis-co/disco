var Base = require('../base');

var ProcessorView = Base.ItemView.extend({
  template: require('./templates/processor.hbs'),

  templateHelpers: function() {
    return {
      Total: this.model.get("Processor")['_Total']
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

module.exports = ProcessorView;
