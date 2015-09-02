var _       = require('underscore');
var Base    = require('../base');
var Project = require('../../../models/project.js');
var Cluster = require('../../../models/cluster.js');

module.exports = Base.ItemView.extend({
  template: require('./templates/modal.hbs'),
  className: 'modal fade',

  events: {
    'click button.cancel':      'close',
    'click button.close':       'close',
    'click button.create':      'create',
    'keyup input#project-name': 'update',
    'hidden.bs.modal':          'destroy'
  },

  ui: {
    name: 'input#project-name',
    db:   'input#database-name'
  },

  initialize: function(options) {
    if(typeof options === 'undefined') {
      this.collection = new Project.Collection();
      this.collection.fetch();
    } else {
      this.collection = options.collection;
    }
  },
  
  sanitize: function(str) {
    return str.toLowerCase()
      .replace(/[^0-9a-zA-Z]/g, "_")
      .replace(/__+/g, "_")
      + '_data';
  },

  update: function() {
    this.ui.db.val(this.sanitize(this.ui.name.val()));
  },
  
  create: function() {
    this.collection.each(function(project) {
      project.save({ Loaded: false  });
    });

    var project = new Project.Model({
      Loaded:   true,
      Database: this.sanitize(this.ui.name.val()),
      Name:     this.ui.name.val(),
      Created:  new Date(),
      Updated:  new Date()
    });

    project.save({},{
      success: _.bind(function () {
        project.createCluster(_.bind(function (err, cluster) {
          if(err) $.growl("Could not save cluster.", { type: 'danger' });
          this.collection.add(project);
          this.close();
        },this));
      },this),
      error: function () {
        console.log('error saving project: ', arguments);
        $.growl("There was an error during creation of project.<br/>Does it already exist?", { type: 'danger'});
      }
    });
  },

  close: function() {
    this.collection = null;
    this.$el.modal('hide');
  },

  onShow: function() {
    this.$el.modal();
  }
});
