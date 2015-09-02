var Channel  = require('../../src/js/lib/channels.js');
var Radio    = require('backbone.radio');
var Backbone = require('backbone');

var Base1 = Channel.infect(Backbone.Model).extend({
  defaults: {
    value: 0
  },
  channels: {
    "nop": "nop"
  },
  actions: {
    "event/actions/test": function() {
      this.set('value', 30);
    },
    "event/actions/more": function() {
      this.set('value', 80);
    }
  }
});

var Base2 = Base1.extend({
  channels: {
    "yep": "yep"
  },
  actions: {
    "event/actions/whatever": function() {
      this.set('value', 30);
    },
    "event/actions/suits": function() {
      this.set('value', 80);
    },
    "event/actions/you": function() {
      this.set('value', 30);
    }
  }
});

describe("Channel Mixin Inheritance", function() {
  it('should inherit all actions from parent', function() {
    var item1 = new Base1();
    var item2 = new Base2();
    expect(Object.keys(item1.channels).length).toBe(2);
    expect(Object.keys(item2.channels).length).toBe(3);
  });

  it('should inherit all actions from parent', function() {
    var item1 = new Base1();
    var item2 = new Base2();
    expect(Object.keys(item1.actions).length).toBe(4); // + 2 default actions defined in channels.js
    expect(Object.keys(item2.actions).length).toBe(7); // + 2 default actions defined in channels.js
  });

  it("should instantiate an action object per instance", function() {
    var inst1 = new Base1();
    var inst2 = new Base2();

    var channel = Radio.channel('actions');
    channel.trigger('test');
    channel.trigger('more');  //  :)
    channel.trigger('suits');
    expect(inst1.get('value')).toBe(80);
    expect(inst2.get('value')).toBe(80);
  });

  it("should be neat and keep Backbone clean", function() {
    expect(Backbone.Model.prototype.actions).toBeUndefined();
    expect(Backbone.Model.prototype.channels).toBeUndefined();
  });
});
