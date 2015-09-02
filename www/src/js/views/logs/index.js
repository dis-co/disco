var Backbone = require('backbone.marionette');
var Handlebars = require('handlebars');

var Row = Backbone.ItemView.extend({
  template: require('./templates/log.hbs'),

  templateHelpers: function() {
    var level = "default";

    switch(this.model.get("LogLevel")) {
    case "Debug":
      level = "default";
      break;
    case "Info":
      level = "success";
      break;
    case "Warning":
      level = "warning";
      break;
    case "Error":
      level = "danger";
      break;
    case "Fatal":
      level = "danger";
      break;
    }
    
    return {
      level: level
    };
  }
});

module.exports = Backbone.CollectionView.extend({
  childView: Row,

  initialize: function() {
    this.listenTo(this.collection, 'add', function() {
      if(this.collection.size() > 500) this.collection.reset();
    });
  }
});
