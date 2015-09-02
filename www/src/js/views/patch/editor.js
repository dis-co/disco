var Base    = require('../components/base');
var Patch   = require('../components/patch');

var SideBar = require('./sidebar.js');
var ToolBar = require('./toolbar.js');

var Editor = Base.LayoutView.extend({
  template: require('./templates/editor.hbs'),
  className: 'row editor-container',

  regions: {
    'sidebar': '.sidebar',
    'toolbar': '.toolbar-wrapper',
    'hostbar': '.hostbar-wrapper',
    'editor':  '.editor'
  },

  initialize: function(options) {
    this.filterids = options.ids;
    this.cues      = options.cues;
    this.patches   = options.patches;
  },

  onRender: function() {
    this.sidebar.show(new SideBar({
      collection: this.cues
    }));
    this.toolbar.show(new ToolBar({
      collection: this.patches,
      cues: this.cues
    }));
    this.editor.show(new Patch.List({
      ids: this.filterids,
      collection: this.patches
    }));
  }
});

module.exports = Editor;
