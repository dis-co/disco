var Base = require('../components/base');
var Cue  = require('../components/cue');

var CueRegion = Base.LayoutView.extend({
  className: 'row cue-region-wrapper',

  template: require('./templates/cue-region.hbs'),

  regions: {
    cues: '.cues'
  },

  onRender: function() {
    this.cues.show(new Cue.List({
      collection: this.collection,
      draggable:  true
    }));
  } 
});

module.exports = CueRegion;
