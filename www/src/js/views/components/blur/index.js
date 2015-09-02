var Backbone = require('backbone.marionette');

module.exports = Backbone.ItemView.extend({
  className: 'blur-overlay',
  template:  require('./templates/blur.hbs')
});
