var Base       = require('../components/base');
var EditRegion = require('./edit-region.js');
var CueRegion  = require('./cue-region.js');
var ListRegion = require('./cuelist-region.js');


var Editor = Base.LayoutView.extend({
  className: 'row cuelists-editor',
  template: require('./templates/editor.hbs'),

  regions: {
    cuesRegion: '.cues-region',
    editRegion: '.edit-region',
    listRegion: '.cuelists-region'
  },
  
  initialize: function(options) {
    this.cues     = options.cues;
    this.cuelists = options.cuelists;
  },

  onRender: function() {
    this.cuesRegion.show(new CueRegion({
      collection: this.cues
    }));

    this.listRegion.show(new ListRegion({
      collection: this.cuelists
    }));

    this.editRegion.show(new EditRegion({
      collection: this.cuelists
    }));
  }
});

module.exports = Editor; 
