var Channel  = require('../../src/js/lib/channels.js');
var Radio    = require('backbone.radio');
var Backbone = require('backbone');

var Base1 = Channel.infect(Backbone.Model).extend({
  defaults: {
    value: 0
  },
  actions: {
    "event/actions/test": function() {
      this.set('value', 10);
    }
  },
  channels: {
    cues: 'cues'
  },
  initialize: function() {
    this.listenTo(this, 'change', function() {
      this.set({ value: 20 }, { silent: true });
    });
  }
});

var Base2 = Channel.infect(Backbone.Model).extend({
  defaults: {
    value: 0
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

describe("Channel Mixin", function() {
  it('should add channels as per definition', function() {
    var inst1 = new Base1();
    var inst2 = new Base2();
    expect(inst1.cues).toBe(Radio.channel('cues'));
    expect(inst2.cues).toBeUndefined();
  });

  it('should delete channels as per definition', function() {
    var inst1 = new Base1();
    expect(inst1._listeningTo).not.toEqual({}); // should contain one event!
    inst1.stopListening();                      // now we remove everything
    expect(inst1.cues).toBeUndefined();         // cues should get deleted
    expect(inst1._listeningTo).toEqual({});     // and regular event handlers removed
  });
});
