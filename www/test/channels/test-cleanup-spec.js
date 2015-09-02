var Channel  = require('../../src/js/lib/channels.js');
var Radio    = require('backbone.radio');
var Backbone = require('backbone');

var Base1 = Channel.infect(Backbone.Model).extend({
  defaults: {
    value: 0
  },
  actions: {
    "event/actions/test": function() {
      this.set('value', 90);
    }
  }
});

var Base2 = Channel.infect(Backbone.Model).extend({
  defaults: {
    value: 0
  },
  actions: {
    "event/actions/test": function() {
      this.set('value', 30);
    }
  }
});

/**
 * This test describes the situation caused when not *explicitly* freeing
 * callback by action name, callback *and* context, where, as per Backbone.Radio
 * API, *all* callback to that action get cleaned up.
 *
 * This specifically happens, when, say during navigation from one resource to
 * another, Models or Views of the same type/class get instantiated *before*
 * their siblings get cleaned up.
 */
describe("Channel Mixin Inheritance", function() {
  it('should inherit all actions from parent', function() {
    var item1 = new Base1();
    var item2 = new Base2();

    item1.stopListening();

    var channel = Radio.channel('actions');
    channel.trigger('test');
    
    expect(item1.get('value')).toBe(0);
    expect(item2.get('value')).toBe(30);
  });
});
