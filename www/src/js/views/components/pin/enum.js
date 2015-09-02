var _          = require('underscore');
var Base       = require('./base.js');
var Collection = require('backbone').Collection;

var Slice = Base.Slice.extend({
  template: require('./templates/enum/slice.hbs'),

  events: {
    'change select': 'select'
  },

  initialize: function(options) {
    var pin = this.model.collection.pin;
    this.listenTo(pin, 'iris.updates', function(m) {
      this.model.set({ Value: pin.getAt(this.idx) });
      this.render();
    });
  },

  select: function(event) {
    this.model.collection.pin.updateAt(this.idx, $(event.target).val());
  },

  templateHelpers: function() {
    return {
      Properties: this.model.collection.pin.get('Properties')
    };
  },

  onRender: function() {
    this.$el.find('select').val(this.model.get('Value'));
    this.$el.find('select').select2();
  },

  setIdx: function(idx) {
    this.idx = idx;
  }
});

var Enum = Base.Pin.extend({
  childView: Slice,

  initialize: function(options) {
    this.collection = new Collection(this.model.get('Values'));
    this.collection.pin = this.model;
  },

  attachHtml: function(collectionView, childView, idx) {
    childView.setIdx(idx);
    collectionView.$el.find(this.childViewContainer).append(childView.el);
  }
});

module.exports.None = Enum;
