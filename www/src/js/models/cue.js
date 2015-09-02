/**
 * <pre>
 *         ___           ___           ___     
 *        /\  \         /\__\         /\  \    
 *       /::\  \       /:/  /        /::\  \   
 *      /:/\:\  \     /:/  /        /:/\:\  \  
 *     /:/  \:\  \   /:/  /  ___   /::\~\:\  \ 
 *    /:/__/ \:\__\ /:/__/  /\__\ /:/\:\ \:\__\
 *    \:\  \  \/__/ \:\  \ /:/  / \:\~\:\ \/__/
 *     \:\  \        \:\  /:/  /   \:\ \:\__\  
 *      \:\  \        \:\/:/  /     \:\ \/__/  
 *       \:\__\        \::/  /       \:\__\    
 *        \/__/         \/__/         \/__/    
 * </pre>
 *
 * A named snapshot of the state of selected _VVVV IOPins_ at creation/edit time.
 */
var _      = require('underscore');
var Base   = require('./base.js');
var async  = require('async');
var Radio  = require('backbone.radio');
var Tags   = require('./tag.js');
var Editor = require('../views/components/cue/edit.js');
/**
 * ### Name
 *
 * The default name component when creating new cues in the editor. This gets
 * combined with a number to make up the initial name.
 */
var DEFAULT_NAME = 'Cue';

/**
 * <pre>
 *  __  __           _      _ 
 * |  \/  | ___   __| | ___| |
 * | |\/| |/ _ \ / _` |/ _ \ |
 * | |  | | (_) | (_| |  __/ |
 * |_|  |_|\___/ \__,_|\___|_|
 * </pre>
 *
 *
 */
var Cue =  Base.Model.extend({
  url: 'iris.cues',

  idAttribute: '_id',

  defaults: {
    Project: '',
    Name: '',
    Type: 'Cue',
    Tags: [],
    Hosts: [],
    Values: [],
    ExecFrame: -1
  },

  channels: {
    cue:  'cue',
    cues: 'cues'
  },

  actions: {
    'request/cue/play/:id': function() {
      this.play();
    },
    'request/cue/attributes/:id': function() {
      return this.attributes;
    }
  },

  /**
   * ### editing
   *
   * Editing state of current Cue. This lives outside of the attrbiutes hash of
   * the model, as there is no need to synchronize this flag with other clients
   * (yet).
   */ 
  editing: false,

  /**
   * ### edit
   *
   * Set: put this Cue into edit mode. This means:
   * 
   * - we set the `editing` state field to true
   * - signal all interested parties (pins, in particular) that they
   *   should enter the selectable state
   * - use `play` to set all pins to the current values this cue holds
   * - and finally request those pins to set themselves into the `selected`
   *   state, whose values are part the current cue
   */
  edit: function() {
    this.editing = true;
    this.cues.trigger('selectable');
    _.each(this.get('Values'), function(value) {
      this.cues.trigger(
        'set/' + encodeURIComponent(value.Target).toLowerCase(),
        value.Values);
      this.cues.trigger(
        'select/' + encodeURIComponent(value.Target).toLowerCase());
    }, this);
  },

  activate: function() {
    _.each(this.get('Values'), function(value) {
      this.cues.trigger(
        'set/' + encodeURIComponent(value.Target).toLowerCase(),
        value.Values);
    }, this);
  },

  /**
   * ### cancel
   *
   * Set: cancel any ongoing editing process.
   */
  cancel: function() {
    this.editing = false;
    this.trigger('cancel');
  },

  /**
   * ### play
   *
   * Play the current cue. In particular, this means setting the `Trigger`
   * fields to true and sending the Cue to the server.
   */
  play: function() {
    this.save({ Trigger: true });
  }
});

/**
 * <pre>
 *   ____      _ _ _           _   _             
 *  / ___|___ | | | | ___  ___| |_(_) ___  _ __  
 * | |   / _ \| | | |/ _ \/ __| __| |/ _ \| '_ \ 
 * | |__| (_) | | | |  __/ (__| |_| | (_) | | | |
 *  \____\___/|_|_|_|\___|\___|\__|_|\___/|_| |_|
 * </pre>
 *
 *
 */
var Cues = Base.Collection.extend({
  url: 'iris.cues',
  model: Cue,

  comparator: 'Name',

  channels: {
    cues: 'cues',
    pins: 'pins'
  },

  initialize: function(options) {
    this.project = options ? options.project : '';
  },

  /**
   * ### new
   *
   * Set: convenience method to set interested parties (e.g. IOPins) into
   * `selectable` mode.
   */
  new: function() {
    this.cues.trigger('selectable');
  },

  /**
   * ### save
   *
   * Set: collect all data from selected parties, create a new Cue model from
   * it if not editing one, and save fetched data to it.
   */
  save: function() {
    var model = _.findWhere(this.models, { editing: true });
    var data  = this.getData();

    if(model) {
      model.save({ Values: data });
    } else {
      var cue = new Cue({
        Project: this.project || '',
        Name: this.getDefaultName(),
        Values: data
      });
      cue.save({}, {
        success: _.bind(function (model) {
          this.add(model);
          new Editor({ model: cue }).show();
        },this),
        error: function(arg) {
          $.growl("Error saving cue: " + arg[0], { type: 'danger' });
        }
      });
    }
    this.cancel();
  },

  /**
   * ### cancel
   *
   * Set: cancel any ongoing editing/creation process.
   */
  cancel: function() {
    this.each(function(cue) { cue.cancel(); });
    this.cues.trigger('cancel');
  },

  getTags: function() {
    var collection = new Tags.Tags();

    collection.add(new Tags.Tag({ Tag: 'All Cues' }));
    collection.add(new Tags.Tag({ Tag: 'No Tags' }));

    return this.reduce(function(memo, cue) {
      _.map(cue.get('Tags'), function(tag) {
        if(!memo.has(tag)) {
          memo.add(new Tags.Tag({ Tag: tag }));
        }
      });
      return memo;
    }, collection);
  },

  /**
   * ### getData
   *
   * Get: retrieve all selected pins' data, and return a compacted array of it.
   */
  getData: function() {
    var ids = _.uniq(this.pins.request('ids'));

    // collect all data
    var data = _.compact(_.map(ids, function(id) {
      return this.cues.request('save/' + id.pin);
    }, this));

    // deduplicate data
    return _.reduce(data, function (res, val) {
      if(_.isEmpty(_.where(res, { Target: val.Target })))
        res.push(val);
      return res;
    }, []);
  },

  /**
   * ### getDefaultName
   *
   * Get: contruct a new cue name by concanating the default name (above) with a
   * (possibly guessed) monotonically increasing sequence number.
   */
  getDefaultName: function() {
    var last = _.last(this.filter(function(cue) {
      return cue.get('Name').match(new RegExp(DEFAULT_NAME));
    }).sort());

    if(last) {
      var parsed = last.get('Name').split(" ");
      var num;
      try {
        num = parseInt(parsed[parsed.length - 1], 10);
      } catch(e) {
        num = 1;
      }
      return DEFAULT_NAME + " " + (num + 1);
    } else {
      return DEFAULT_NAME + " 1";
    }
  }
});

module.exports.Model      = Cue;
module.exports.Collection = Cues;
