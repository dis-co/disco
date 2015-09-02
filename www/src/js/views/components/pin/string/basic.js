var _          = require('underscore');
var Base       = require('../base.js');
var Backbone   = require('backbone');
var Marionette = require('backbone.marionette');
var PathInfo   = require('../../../../models/fs.js');

var animationEvents = 'webkitAnimationEnd mozAnimationEnd MSAnimationEnd oanimationend animationend';

var Slice = Base.Slice.extend({
  tagName: 'div',
  className: 'placard',
  attributes: {
    'data-ellipsis': 'true',
    'data-initialize': 'placard'
  },

  initialize: function(options) {
    this.pin = this.model.get('pin');
    this.listenTo(this.pin, 'iris.updates', function() {
      this.$el.placard('setValue', this.pin.getAt(this.model.get('index')));
    });
  },

  validate: function() {
    return true;
  },

  onRender: function() {
    this.$el.placard({
      explicit: false,
      revertOnCancel: true,
      externalClickAction: 'cancel',
      onAccept: _.bind(function(options) {
        if(this.validate(options.value)) {
          this.$el.placard('setValue', options.value);
          this.pin.updateAt(this.model.get('index'), options.value);
          this.$el.placard('hide');
        } else {
          this.$el.one(animationEvents, _.bind(function() {
            this.$el.removeClass('animated shake');
          },this));
          this.$el.addClass('animated shake');
        }
      },this)
    });
    this.$el.placard('setValue', this.model.get('Value'));

    if(this.pin.get('NodePath') === 'debug-string')
      this.$el.placard('disable');
  }
});


var SingleLineSlice = Slice.extend({
  template: require('../templates/string/singleline.hbs')
});

var MultiLineSlice = Slice.extend({
  template: require('../templates/string/multiline.hbs')
});

var UrlSlice = SingleLineSlice.extend({
  validate: function(string) {
    if(string.match(/^(http|https|ftp|rtmp):\/\/[a-z0-9-\.]+.*/)) {
      return true;
    }
    return false;
  }
});

var IPSlice = SingleLineSlice.extend({
  validate: function(string) {
    if(string.match(/^(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$/)) {
      return true;
    }
    return false;
  }
});

var String = Base.Pin.extend({
  initialize: function(options) {
    var values = _.map(this.model.get('Values'), function(val, idx) {
      return _.extend({ index: idx, pin: this.model }, val);
    }, this);
    this.collection = new Backbone.Collection(values);
  },

  getChildView: function() {
    switch(this.model.behavior()) {
    case 'String':
      return SingleLineSlice;
    case 'MultiLine':
      return MultiLineSlice;
    case 'Url':
      return UrlSlice;
    case 'IP':
      return IPSlice;
    default:
      return SingleLineSlice;
    }
  }
});

module.exports.String    = String;    
module.exports.MultiLine = String;
module.exports.Url       = String;
module.exports.IP        = String;


