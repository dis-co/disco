var Channel  = require('../../src/js/lib/channels.js');
var Resource = require('./resource.js');
var Backbone = require('backbone');

describe("Channel Mixin Sync Adapter", function() {
  var resource;
  var uri = 'my.funky.resource';
  var Base = Channel.infect(Backbone.Model).extend({
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
   *  »pull»
   * <pre>                _       
   *   ___ _ __ ___  __ _| |_ ___ 
   *  / __| '__/ _ \/ _` | __/ _ \
   * | (__| | |  __/ (_| | ||  __/
   *  \___|_|  \___|\__,_|\__\___|
   * </pre>                             
   */
  it("it should create model from data and assign id", function(done) {
    resource.respondTo('create').with({ id: '123456789' });

    var inst = new Base();
    expect(inst.id).toBeUndefined();

    inst.save({ value: 'present' }, {
      success: function() {
        expect(inst.id).toBe('123456789');
        expect(inst.get('value')).toBe('present');
        done();
      }
    });
  });

  /**
   *  »pull»
   * <pre>               _ 
   *  _ __ ___  __ _  __| |
   * | '__/ _ \/ _` |/ _` |
   * | | |  __/ (_| | (_| |
   * |_|  \___|\__,_|\__,_|
   * </pre>                
   */
  it("it should read and parse data to model which exists", function(done) {
    resource.respondTo('read').on('test-123').with({ value: 'present' });

    var inst = new Base({ id: 'test-123' });

    expect(inst.get('value')).toBeUndefined();

    inst.fetch({
      success: function() {
        expect(inst.get('value')).toBe('present');
        done();
      }
    });
  });

  it("it should call error when model does not exists", function(done) {
    resource.respondTo('read').on('test-123').with({ value: 'present' });
    
    var inst = new Base({ id: 'huzzah' });

    inst.fetch({
      error: function(err) {
        expect(err).toBeTruthy();
        done();
      }
    });
  });

  it("it should call error when endpoint does not exists", function(done) {
    resource.respondTo('read').on('test-123').with({ value: 'present' });
    
    var WrongBase = Channel.infect(Backbone.Model).extend({
      url: uri + 'failure'
    });

    var inst = new WrongBase({ id: 'test-123' });

    inst.fetch({
      error: function(err) {
        expect(err).toBeTruthy();
        done();
      }
    });
  });


  /**
   *  »pull»
   * <pre>            _       _       
   *  _   _ _ __   __| | __ _| |_ ___ 
   * | | | | '_ \ / _` |/ _` | __/ _ \
   * | |_| | |_) | (_| | (_| | ||  __/
   *  \__,_| .__/ \__,_|\__,_|\__\___|
   *       |_|                        
   * </pre>
   */
  it("it should update data of existing model", function(done) {
    resource.respondTo('read').on('update-test-123').with({ name: 'karsten'});
    resource.respondTo('update').on('update-test-123');

    var inst = new Base({ id: 'update-test-123' });

    inst.fetch({
      success: function() {
        expect(inst.id).toBe('update-test-123');
        expect(inst.get('name')).toBe('karsten');
        inst.save({ name: 'torsten' }, {
          success: function() {
            expect(inst.get('name')).toBe('torsten');
            done();
          }
        });
      }
    });
  });


  /**
   *  »pull»
   * <pre>
   *      _      _      _       
   *   __| | ___| | ___| |_ ___ 
   *  / _` |/ _ \ |/ _ \ __/ _ \
   * | (_| |  __/ |  __/ ||  __/
   *  \__,_|\___|_|\___|\__\___|
   * </pre>
   */
  it("it should delete model", function() {
    var spy = jasmine.createSpy('destroy-event');
    resource.respondTo('delete').on('test-123');

    var inst = new Base({ id: 'test-123' });
    inst.on('destroy', spy);
    inst.destroy();

    expect(spy).toHaveBeenCalled();
  });


  /** »push»
   *  <pre>
   *                  _       _       
   *  _   _ _ __   __| | __ _| |_ ___ 
   * | | | | '_ \ / _` |/ _` | __/ _ \
   * | |_| | |_) | (_| | (_| | ||  __/
   *  \__,_| .__/ \__,_|\__,_|\__\___|
   *       |_|                        
   * </pre>
   */
  it('should update model attributes via push', function() {
    var inst1 = new Base({ id: 'test-123', name: 'karsten' });
    var inst2 = new Base({ id: 'test-456', name: 'uwe' });

    resource.pushTo('update/'+inst1.id, { name: 'torsten' });

    expect(inst1.get('name')).toBe('torsten');
    expect(inst2.get('name')).toBe('uwe');
  });

  /** »push»
   *  <pre>
   *      _      _      _       
   *   __| | ___| | ___| |_ ___ 
   *  / _` |/ _ \ |/ _ \ __/ _ \
   * | (_| |  __/ |  __/ ||  __/
   *  \__,_|\___|_|\___|\__\___|
   * </pre>
   */
  it('should delete model via push', function() {
    var inst = new Base({ id: 'test-123', name: 'karsten' });
    var spy = jasmine.createSpy('delete-spy');
    inst.on('destroy',spy);
    resource.pushTo('delete/'+inst.id);
    expect(spy).toHaveBeenCalled();
  });
});
