var _        = require('underscore');
var Base     = require('../base');
var Cue      = require('./cue.js');
var List     = require('./list.js');
var Settings = require('../../../app/settings.js');

var Grouped = Base.LayoutView.extend({
  className: 'grouped-item',

  template: require('./templates/group-item.hbs'),

  templateHelpers: function() {
    return {
      Tag: this.model.get("Tag")
    };
  },

  ui: {
    'handle': 'span.handle',
    'list':   '.cue-list'
  },

  events: {
    'click .handle': 'toggle'
  },

  regions: {
    'list': '.cue-list'
  },

  initialize: function(options) {
    var tags = Settings.get('tags') || {};

    if(tags[this.model.get('Tag')]) {
      this.isOpen = tags[this.model.get('Tag')].open;
    } else {
      this.isOpen = false;
    }

    this.listenTo(this, "before:destroy", function (arg) {
      this.collection = null;
    });

    this.options = options;
  },

  onRender: function() {
    var list = new List(_.extend({
      tag: this.model.get("Tag"),
      collection: this.options.collection
    },this.options));

    this.listenTo(list, 'edit', function() {
      this.trigger('edit');
    }, this);

    this.list.show(list);

    if(this.isOpen) {
      this.openDrawer();
    } else {
      this.closeDrawer();
    }
  },

  toggle: function() {
    if(this.isOpen) {
      this.closeDrawer();
    } else {
      this.openDrawer();
    }
  },

  openDrawer: function() {
    this.isOpen = true;
    this.setHandle();
    this.ui.list.removeClass('hidden');
    this.saveState();
  },

  closeDrawer: function() {
    this.isOpen = false;
    this.setHandle();
    this.ui.list.addClass('hidden');
    this.saveState();
  },

  saveState: function() {
    var tags = Settings.get('tags') || {};
    tags[this.model.get('Tag')] = { open: this.isOpen };
    Settings.save('tags', tags);
  },

  setHandle: function() {
    if(this.isOpen) {
      this.ui.handle.removeClass('fa-chevron-right');
      this.ui.handle.addClass('fa-chevron-down');
    } else {
      this.ui.handle.removeClass('fa-chevron-down');
      this.ui.handle.addClass('fa-chevron-right');
    }
  }
});

var GroupedList = Base.CollectionView.extend({
  className: 'group-list',

  childView: Grouped,

  initialize: function(options) {
    this.options = options;
    this.listenTo(this.options.cues, 'change:Tags', this.update);
  },

  update: function() {
    var tags = this.options.cues.getTags();

    this.collection.trigger('caution');
    
    this.collection.reset();
    
    tags.each(function(model) {
      var existance = this.collection.findWhere(model.attributes);
      if(typeof existance === 'undefined') {
        this.collection.add(model);
      }
    }, this);
  },
  
  buildChildView: function(child, ChildViewClass, childViewOptions) {
    var options = _.extend({
      model:      child,
      collection: this.options.cues,
      editable:   this.options.editable,
      trashable:  this.options.trashable
    }, childViewOptions);

    var view = new ChildViewClass(options);

    this.listenTo(view, 'edit', function() {
      this.trigger('edit');
    }, this);

    return view;
  }
});

module.exports = GroupedList;
