var _        = require('underscore');
var Backbone = require('backbone');
var moment   = require('moment');
var Base     = require('../base.js');
var Fs       = require('../../../../models/fs.js');

var Slice = Base.Slice.extend({
  className: 'filepicker slice',

  template: require('../templates/string/path-input.hbs'),

  events: {
    'click button.up':    'up',
    'click button.open':  'open',
    'click button.home':  'home',
    'click button.save':  'save',
    'click button.drive': 'drive'
  },

  /**
   * ### Initialize
   * - set a shortcut to the Pin instance
   * - register a listener for _iris.updates_ events to update the fields
   * - pre-render the templates
   */
  initialize: function() {
    this.drives = new Fs.Drives();
    
    this.pin = this.model.get('pin');

    this.listenTo(this.pin, 'iris.updates', function() {
      this.setValue(this.pin.getAt(this.model.get('index')));
    });

    // -------- Templates ---------
    this.driveTmpl    = require('../templates/string/drive.hbs');
    this.treeTmpl     = require('../templates/string/tree.hbs');
    this.dirInfoTmpl  = require('../templates/string/dir.hbs');
    this.fileInfoTmpl = require('../templates/string/file.hbs');
  },

  /**
   * ### onRender
   * - save the reference to the $modal DOM element for later use
   * - register callback to execute when modal is _shown_ (renders file-tree)
   * - register callback to execute when modal is _closed_ (cleans up file-tree)
   */
  onRender: function() {
    // -------- Global Handle to Dialog --------
    this.$modal = this.$el.find('.fs-modal');
    var $actions = this.$modal.find('.actions');

    // populate the drives bar with entries from host
    this.drives.fetch({
      reset: true,
      success: _.bind(function(drives) {
        drives.each(_.bind(function(drive) {
          $actions.prepend(this.driveTmpl({ drive: drive.get('Letter') }));
        },this));
      },this)
    });

    this.$modal.on('shown.bs.modal', _.bind(function() {
      this.renderTree(this.basePath);
    },this));

    this.$modal.on('hidden.bs.modal', _.bind(function() {
      this.cleanup();
    },this));
  },

  /**
   * ### up
   * - set the current _basePath_ field to the value of _parentPath_
   * - re-render the file-tree
   */
  up: function(event) {
    event.stopPropagation();
    this.basePath = this.parentPath;
    this.renderTree();
  },

  /**
   * ### home
   * - set the current _basePath_ field to a fixed value (e.g. `/home/k/`)
   * - re-render the file-tree
   */
  home: function(event) {
    event.stopPropagation();
    this.basePath = 'C:\\Iris';
    this.renderTree();
  },

  /**
   * ### root
   * - set the current _basePath_ field to the root of the file system (e.g. '/' on *nix)
   * - re-render the file-tree
   */
  drive: function(event) {
    var bp = $(event.target).attr('id');
    this.basePath = bp;
    this.renderTree();
  },

  /**
   * ### save
   * - get the items selected in the currently shown file-tree
   * - update the Pin value at the index specified during rendering of this view
   * - set the _basePath_ field to the new parent path used for navigation
   * - set the view elements to new path
   * - hide the modal dialog
   */
  save: function(event) {
    event.stopPropagation();
    var items = this.getTree().tree('selectedItems');
    this.pin.updateAt(this.idx, items[0].Path);
    this.basePath = (items[0].type === 'item') ? items[0].Parent : items[0].Path;

    var value = items[0].Path;

    this.setValue(value);
    this.model.get('pin').updateAt(this.model.get('index'), value);
    this.$modal.modal('hide');
  },

  /**
   * ### open
   * - show the modal dialog containing the file tree widget
   */
  open: function(event) {
    event.stopPropagation();
    this.$modal.modal();
  },

  /**
   * ### cleanup
   * - remove the tree DOM elements 
   */
  cleanup: function() {
    this.$el.find('button.save').attr('disabled', 'disabled');
    this.rmTree();
  },

  /**
   * ### toggleUp
   * - disable `up` button if the file system root has been reached
   * - enable otherwise
   */
  toggleUp: function() {
    if(this.basePath === '/') {
      this.$modal.find('button.up').attr('disabled', 'disabled');
    } else {
      this.$modal.find('button.up').attr('disabled', null);
    }
  },

  /**
   * ### renderTree
   * - toggle the enabled state of the up button
   * - remove the tree (if one exists)
   * - make a new tree DOM snippet
   * - re-initialize the widget with the current `basePath` field
   */ 
  renderTree: function() {
    this.rmTree();
    this.mkTree();
    this.initTree(this.basePath);
    this.toggleUp();
  },

  /**
   * ### initTree
   * @param {string} path - (optional) path to pass to `dataSource` function
   * 
   * - get the tree element
   * - invoke the `tree` function on it, thereby creating the widget with a 
   *   newly create callback function (`treeSource`).
   * - register a callback to show detailed information about the file/folder
   *   in the right-hand section of the dialog.
   */
  initTree: function(path) {
    var $el = this.getTree();

    $el.tree(_.extend({
      dataSource: this.treeSource(path)
    }, this.treeOptions));

    $el.on('selected.fu.tree', _.bind(function(event, data) {
      this.$el.find('button.save').attr('disabled', null);
      this.updateInfo(data.target);
    },this));

    return $el;
  },

  /**
   * ### mkTree
   * - contruct a tree DOM element from the template
   * - add element to DOM and return it
   */
  mkTree: function() {
    var $el = $(this.treeTmpl());
    this.$modal.find('.treecup').html($el);
    return $el;
  },

  /**
   * ### getTree
   * - get the tree widget DOM node 
   */
  getTree: function() {
    return this.$modal.find("ul.tree");
  },

  /**
   * ### rmTree
   * - get the tree DOM node
   * - remove listener
   * - remove tree node itself
   */
  rmTree: function() {
    var $tree = this.getTree();
    $tree.off('selected.fu.tree');
    $tree.tree('destroy');
  },

  /**
   * ### treeSource
   * construct a callback function designed for the fluelux tree widget
   *
   * @param {string} path - (optional) path to use a `basePath`
   *
   * #### Notes
   * - Determine the `basePath` field and set it either to `path` passed
   *   (priority) or to the value of the Pin at the slice index registered by
   *   this view.
   * 
   * - Construct and return a function closing over the current Input view
   *   instance. A PathInfo is instantiated either with the view's `basePath`,
   *   or the Path contained in the options object.
   *
   * - Fetch and format file system data, invoking the widgets callback to begin
   *   rendering the tree. The parent's path is saved to a field in the view to
   *   facilitate the functionality of the `up` button.
   */
  treeSource: function(path) {
    var view = this;

    view.basePath = (typeof path === 'undefined')
      ? view.pin.getAt(view.model.get('index'))
      : path;

    // return a callback closing over the basepath which we start with.
    return function(options, cb) {
      var info;

      if(typeof options.type === 'undefined') {
        info = new Fs.PathInfo({ Path: view.basePath });
      } else {
        view.basePath = options.Root;
        info = new Fs.PathInfo({ Path: options.Path });
      }

      info.treeData(options, function(response) {
        view.updateInfo(response.current);
        view.parentPath = response.parent;
        view.basePath   = response.current.Path;
        cb(response);
      });
    };
  },

  /**
   * ### setValue
   * - set display elements to the current paths
   */ 
  setValue: function(value) {
    this.$el.find('span#current-path').html(value);
    this.$el.find('input[type=text]').val(value);
  },

  /**
   * ### updateInfo
   * @param {object} data - object containing a `target` key containing data of
   *                        selected file or folder.
   * 
   * - get the `side-panel` element
   * - depending on the type of the currently selected element, render a
   *   template and append to `side-panel`
   */
  updateInfo: function(data) {
    var $box = this.$el.find('.side-panel');
    var format = "DD/MM/YYYY<br> h:mm a";

    if(typeof data === 'undefined') {
      $box.html('Not found');
      return;
    }

    var parsed = _.extend({
      Created:  moment(data.CreatedAt).format(format),
      Accessed: moment(data.LastAccessAt).format(format),
      Written:  moment(data.LastWriteAt).format(format)
    }, data);

    switch(data.type) {
    case 'folder':
      $box.html(this.dirInfoTmpl(parsed));
      break;
    case 'item':
      $box.html(this.fileInfoTmpl(parsed));
      break;
    }
  },

  setTreeOptions: function(options) {
    this.treeOptions = options;
  }
});

/**
 * ### Base Colletion view for Pin
 */
var View = Base.Pin.extend({
  childView: Slice,

  templateHelpers: function() {
    return {
      Pin: this.model.attributes
    };
  },

  initialize: function(options) {
    var values = _.map(this.model.get('Values'), function(value, idx) {
      return _.extend({
        index: idx,
        pin: this.model
      }, value);
    }, this);
    this.collection  = new Backbone.Collection(values);
    this.treeOptions = options.treeOptions;
  }
});

var FileName = View.extend({
  treeOptions: {
    mode: 'item',
    cacheItems: false,
    folderSelect: false
  }
});

var Directory = View.extend({
  treeOptions: {
    mode: 'folder',
    cacheItems: false,
    folderSelect: true,
    itemSelect: false
  }
});

module.exports.FileName  = FileName;
module.exports.Directory = Directory;
