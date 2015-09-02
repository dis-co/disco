var Base    = require('../components/base');
var Cuelist = require('../components/cuelist');

var CuelistRegion = Base.LayoutView.extend({
  className: 'row',
  template: require('./templates/cuelist-region.hbs'),

  regions: {
    cuelists: '.cuelists'
  },

  events: {
    'click button.add-cuelist': 'add'
  },

  onRender: function() {
    this.cuelists.show(new Cuelist.List({
      collection: this.collection
    }));
  },

  add: function() {
    this.collection.new();
  }
});

module.exports = CuelistRegion;
