var Channel  = require('../../src/js/lib/channels.js');
var Resource = require('./resource.js');
var Backbone = require('backbone');
var Radio    = require('backbone.radio');

describe("Channel mixin sync adapter on collections", function() {
  var resource;
  var uri = 'my.funky.resource';

  var Base = Channel.infect(Backbone.Collection).extend({
    url: uri
  });

  beforeEach(function() {
    resource = new Resource(uri);
  });
  
  afterEach(function() {
    if (resource) {
      resource.dispose();
    }
  });

  /**
   * <pre>
   *  »pull»             _ 
   *  _ __ ___  __ _  __| |
   * | '__/ _ \/ _` |/ _` |
   * | | |  __/ (_| | (_| |
   * |_|  \___|\__,_|\__,_| [collection data]
   * </pre>
   */
  it("it should read all model data", function(done) {
    resource.respondTo('read').with([
      { id: '123456789' },
      { id: '987654321' }
    ]);

    var coll = new Base();
    expect(coll.size()).toBe(0);

    coll.fetch({
      success: function() {
        expect(coll.size()).toBe(2);
        expect(coll.get('123456789').id).toBeTruthy();
        expect(coll.get('987654321').id).toBeTruthy();
        done();
      }
    });
  });

  /**
   * <pre>
   *  »push»              _       
   *   ___ _ __ ___  __ _| |_ ___ 
   *  / __| '__/ _ \/ _` | __/ _ \
   * | (__| | |  __/ (_| | ||  __/
   *  \___|_|  \___|\__,_|\__\___|
   * </pre>
   */
  it("it should read all model data", function() {
    var coll = new Base();
    expect(coll.size()).toBe(0);
    expect(coll.get('123456789')).toBeUndefined();
    resource.pushTo('create', { id: '123456789', name: 'karsten' });
    expect(coll.size()).toBe(1);
    expect(coll.get('123456789').get('name')).toBe('karsten');
  });
});
