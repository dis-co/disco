/**
 * <pre>
 *           ___           ___     
 *          /\__\         /\__\    
 *         /:/ _/_       /:/ _/_   
 *        /:/ /\__\     /:/ /\  \  
 *       /:/ /:/  /    /:/ /::\  \ 
 *      /:/_/:/  /    /:/_/:/\:\__\
 *      \:\/:/  /     \:\/:/ /:/  /
 *       \::/__/       \::/ /:/  / 
 *        \:\  \        \/_/:/  /  
 *         \:\__\         /:/  /   
 *          \/__/ ile     \/__/ ystem
 * </pre>
 *
 * Tools for walking a file system to display in a fuelux tree widget. Used in
 * particular in views for `String` pins with behavior `FileName` and
 * `Directory`.
 */
var _          = require('underscore');
var async      = require('async');
var Collection = require('./base.js').Collection;
var Model      = require('./base.js').Model;

var Drive = Model.extend({
  idAttribute: 'Letter'
});

var Drives = Collection.extend({
  url: 'iris.host.drives',
  model: Drive
});

/**
 * <pre>
 *  ____       _   _     ___        __       
 * |  _ \ __ _| |_| |__ |_ _|_ __  / _| ___  
 * | |_) / _` | __| '_ \ | || '_ \| |_ / _ \ 
 * |  __/ (_| | |_| | | || || | | |  _| (_) |
 * |_|   \__,_|\__|_| |_|___|_| |_|_|  \___/ 
 * </pre>
 *
 * Connect to `iris.fs` endpoint and retrieve file system data for endpoints
 * specified. Any instance of PathInfo contains a list of its children on-disk.
 */
var PathInfo = Model.extend({
  url: 'iris.host.fs',

  idAttribute: 'Path',

  /**
   * ### initialize
   *
   * Set up the `Children` collection.
   */
  initialize: function(options) {
    this.Children = new Collection();
  },

  /**
   * ### type
   *
   * Get: differentiate whether this instance is a directory or file by checking for
   * the presence of the `Children` attribute.
   */
  type: function() {
    if(typeof this.get('Children') === 'object') {
      return 'folder';
    } else {
      return 'item';
    }
  },

  /**
   * ### toLeaf
   *
   * Return a copy this models attributes and extend it with `type` and `text`
   * properties (used by fuelux).
   */
  toLeaf: function() {
    return _.extend({
      text: this.get('Name'),
      type: this.type()
    }, this.attributes);
  },

  /**
   * ### treeData
   * @param {object} options - options
   * @param {function} callback - function to execute when done fetching data
   *
   * Fetch data for current node on the file system. If the node was found and
   * its was a directory, fetch its child elements (files, folders in that
   * directory..). Finally, call supplied callback with the formatted data for
   * display by the tree widget.
   */
  treeData: function(options, callback) {
    this.fetch({
      success: _.bind(function(pathinfo) {
        if(pathinfo.type() === 'folder') {
          this.fetchChildren(function(err, children) {
            if(err) {
              callback({
                parent: pathinfo.get('Parent'),
                data: [{ 'text': 'Error: ' + JSON.stringify(err), 'type': 'item' }]
              });
            } else {
              var tree = _.map(children, function(child) {
                return child.toLeaf();
              });

              callback({
                current: options.current ? options.current : pathinfo.toLeaf(),
                parent: pathinfo.get('Parent'),
                data: tree
              });
            }
          });
        } else { // its a file ('item'), so recurse to the parent directory
          var parent = new PathInfo({ Path: pathinfo.get('Parent') });
          options.current = pathinfo.toLeaf();
          parent.treeData(options, callback);
        }
      },this),
      error: function(model, err, options) {
        callback({
          data: [{ 'text': err, 'type': 'item' }]
        });
      }
    });
  },

  /**
   * ### fetchChildren
   * @param {function} callback - function to call when all children were
   *                              fetched (or an error occurred)
   *
   * Constructs an array of functions to call in parallel with `async`, invoking
   * `callback` upon completion. Each function constructs a `PathInfo` from the
   * path passed in, and fetches its data.
   */
  fetchChildren: function(callback) {
    var children = this.get('Children');
    if(typeof children === 'object') {
      var tasks = _.map(children, function(item) {
        return _.bind(function(cb) {
          var model = new PathInfo({ Path: item });
          model.fetch({
            success: _.bind(function(model) {
              this.Children.add(model);
              cb(null, model);
            },this),
            error: function(err) {
              cb(err, null);
            }
          });
        }, this);
      }, this);
      async.series(tasks, callback);
    }
  }
});

module.exports.Drives   = Drives;
module.exports.PathInfo = PathInfo;
