var _     = require('underscore');
var Radio = require('backbone.radio');
var Base  = require('../base');
var Patch = require('./patch.js');


var List = Base.CompositeView.extend({
  className: 'patch',

  template: require('./templates/list.hbs'),

  childView: Patch,

  events: {
    'click button.patch-visibility': 'toggle'
  },

  initialize: function(options) {
    this.ids = options.ids;
    this.showAll = true;
    this.listenTo(this.collection, 'filter', this.render);
  },

  toggle: function(event) {
    if(this.showAll) {
      this.children.each(function(view){
        if(view.isVisible) {
          view.hide();
        }
      });
      this.showAll = false;
    } else {
      this.children.each(function(view){
        if(!view.isVisible) {
          view.show();
        }
      });
      this.showAll = true;
    }
  },

  buildChildView: function(child, Patch, childViewOptions) {
    var options = _.extend({
      ids:        this.ids,
      model:      child,
      collection: child.pins
    }, childViewOptions);
    return new Patch(options);
  },

  attachHtml: function(collView, childView, options) {
    collView.$el.append(childView.el);
  },

  filter: function(model) {
    if(this.ids && this.ids.length > 0) {
      var pids = model.pins.map(function(p) { return p.id });
      return _.reduce(this.ids,function(memo, id) {
        if(!memo) memo = pids.indexOf(id) != -1;
        return memo;
      }, false, this);
    }

    // no filtered by host
    if(this.collection.host === 'All') {
      return model.pins.size() > 0;
    }

    // filtered. only render patches with pins
    return (model.pins.size() > 0) &&
      (model.get("HostName") === this.collection.host);
  }
});

module.exports = List;
