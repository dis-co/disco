var Base = require('../components/base');
var Project = require('./project.js');

module.exports = Base.CollectionView.extend({
  className: 'row',

  childView: Project,

  childEvents: {
    load: function(view, id) {
      this.children.forEach(function(child) {
        if(view != child) child.unfocus();
      });
      this.trigger('load', id);
    }
  },

  filter: function(model) {
    if(model.get("Loaded")) return false;
    return true;
  }
});
