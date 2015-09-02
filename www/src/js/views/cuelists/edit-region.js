var _        = require('underscore');
var Backbone = require('backbone');
var Radio    = require('backbone.radio');
var Base     = require('../components/base');

var TmpCueList = Backbone.Collection.extend({
  initialize: function() {
    this.cues = Radio.channel('cue');
  },

  addCue: function(id) {
    var attrs = this.cues.request('attributes/' + id);
    var cue   = new Backbone.Model(attrs);
    this.listenTo(cue, 'destroy', function() {
      this.remove(cue);
    });
    this.add(cue);
  }
});

var EditRegion = Base.LayoutView.extend({
  className: 'row edit-region-wrapper',
  template:  require('./templates/edit-region.hbs'),

  templateHelpers: function() {
    return {
      Name: 'fixme plz'
    };
  },

  regions: {
    list: '.cue-list-region'
  },

  ui: {
    list:   '.cue-list-region',
    header: '.page-header',
    save:   'button.save-cuelist',
    name:   'span.name'
  },

  initialize: function(options) {
    this.listenTo(this.collection, 'select', this.selectList);
  },

  selectList: function(id) {
    this.list.empty();
    
    var model = this.collection.get(id);

    this.ui.list.css({
      height: this.$el.height() - (this.ui.header.outerHeight(true) + 10)
    });

    this.ui.name.addClass('fat');
    this.ui.save.attr('disabled',null);
    this.ui.save
      .addClass('btn-success')
      .removeClass('btn-default');

    var cues = _.foldl(model.get('Cues'), function(coll, id, idx) {
      coll.addCue(id);
      return coll;
    }, new TmpCueList());

    this.list.show(new List({
      model: model,
      collection: cues
    }));
  },

  reset: function() {
    this.ui.name.removeClass('fat');
    this.ui.name.html('Cue List Name');
    this.list.empty();
  }
});

var Cue = Base.ItemView.extend({
  template: require('./templates/cuelist-item.hbs'),

  tagName:  'li',
  className: 'cue-stub',

  events: {
    'click button.play-cue':   'playCue',
    'click button.remove-cue': 'removeCue'
  },

  playCue: function() {
    Radio.channel('cue').request('play/' + this.model.get('Id'));
  },

  removeCue: function(event) {
    if(confirm('Really delete ' + this.model.get('Name') + "?")) {
      this.model.destroy();
    }
  },

  onRender: function() {
    this.$el.attr('id', this.model.get('_id'));
  }
});

var List = Base.CompositeView.extend({
  template: require('./templates/tmp-list.hbs'),

  tagName: 'ul',
  className: 'light list-unstyled',

  childView: Cue,

  initialize: function() {
    this.listenTo(this.collection, 'destroy', function(event) {
      var cues = this.$el.sortable('toArray');
      this.model.save({ Cues: cues });
    }, this);

    this.listenTo(this, 'save', function(ids) {
      this.model.save({ Cues: ids });
    }, this);
  },

  onShow: function() {
    this.$el
      .sortable({
        axis: 'y',
        containment: 'parent',
        placeholder: 'cue-placeholder',
        stop: _.bind(function() {
          var cues = this.$el.sortable('toArray');
          this.$el.empty();
          this.collection.reset();
          _.reduce(cues, function(coll, cue) {
            coll.addCue(cue);
            return coll;
          },this.collection);
          this.trigger('save', cues);
        },this)
      })
      .droppable({});
    this.$el.disableSelection();
  }
});

module.exports = EditRegion;
