var _    = require('underscore');
var Base = require('../base');
var Cue  = require('./cue.js');

var List = Base.CollectionView.extend({
  tagName:   'ul',
  className: 'nav nav-sidebar',
  childView: Cue,

  initialize: function(options) {
    this.tag       = options.tag       || false;
    this.editable  = options.editable  || false;
    this.trashable = options.trashable || false;

    this.listenTo(this, 'before:destroy', function() {
      this.collection = null;
    });
  },

  childEvents: {
    unfocus: function(active) {
      this.children.each(function(child) {
        if(active.model.id != child.model.id) {
          child.unfocus();
        }
      });
    },

    edit: function(editing) {
      this.children.each(function(child) {
        if(editing.model.id != child.model.id) {
          child.model.cancel();
          child.unfocus();
          child.disable();
        }
      });
      this.trigger('edit');
    }
  },

  filter: function(model) {
    if(!this.tag || this.tag === 'All Cues') return true; // don't filter if tag ain't set
    if(model.get("Tags").length === 0 && this.tag === 'No Tags') return true;
    return _.contains(model.get("Tags"), this.tag);
  },

  buildChildView: function(child, ChildViewClass, childViewOptions) {
    var options = _.extend({
      model:     child,
      editable:  this.editable,
      trashable: this.trashable
    }, childViewOptions);
    return new Cue(options);
  }
});

module.exports = List;
