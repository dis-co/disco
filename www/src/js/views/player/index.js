var Base     = require('../components/base');
var TopBar  = require('./topbar.js');
var CuesList = require('./cues.js');

module.exports = Base.LayoutView.extend({
  className: 'row player edit-region-wrapper',
  
  template: require('./templates/layout.hbs'),

  regions: {
    topbar: '.topbar',
    main:   '.editor'
  },

  initialize: function(options) {
    this.options = options;
  },

  onShow: function() {
    this.topbar.show(new TopBar({
      collection: this.options.cuelists
    }));
    this.main.show(new CuesList(this.options));
  }
});
