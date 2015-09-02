var Base    = require('../base');
var Cuelist = require('./cuelist.js');

var List = Base.CollectionView.extend({
  tagName: 'ul',
  className: 'nav nav-sidebar',
  childView: Cuelist,

  childEvents: {
    select: function(selected) {
      this.children.each(function(child) {
        if(selected.model.id != child.model.id) {
          child.deactivate();
        }
      });
      this.collection.trigger('select', selected.model.id);
    }
  }
});

module.exports = List;
