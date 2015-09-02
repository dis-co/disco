var _        = require('underscore');
var models   = require('../../models');
var Base     = require('../components/base');
var Modal    = require('../components/project');
var Cluster  = require('../components/cluster');
var Projects = require('./list.js');
var Project  = require('./project.js');

module.exports = Base.LayoutView.extend({
  template: require('./templates/projects.hbs'),

  className: 'projects',

  ui: {
    'commit': 'button.commit-cluster',
    'member': 'button.new-member',
    'revert': 'button.revert-cluster'
  },

  regions: {
    modals:  '.modals',
    current: '.current',
    cluster: '.cluster',
    recent:  '.recent'
  },

  events: {
    'click button.new-project':    'newProject',
    'click button.new-member':     'newMember',
    'click button.revert-cluster': 'revert',
    'click button.commit-cluster': 'commit'
  },

  initialize: function(options) {
    this.collection = options.collection;
    this.listenTo(this.collection, 'sync destroy', function () {
      this.checkProjects();
      setTimeout(_.bind(function () {
        this.render();
      },this), 500);
    });
  },

  newProject: function() {
    this.modals.show(new Modal({
      collection: this.collection
    }));
  },

  newMember: function(ev) {
    var $el = $(ev.target);
    if($el.hasClass('disabled') || $el.parent().hasClass('disabled')) return;
    new Cluster.Modal({
      model: this.loaded.cluster
    }).show();
  },

  commit: function(event) {
    var $el = $(event.target).hasClass('btn')
          ? $(event.target)
          : $(event.target).parent();

    if(!$el.hasClass('disabled')) {
      new Cluster.Commit({
        model: this.loaded.cluster
      }).show();
    }
  },

  revert: function(event) {
    var $el = $(event.target).hasClass('btn')
          ? $(event.target)
          : $(event.target).parent();

    if(!$el.hasClass('disabled')) {
      new Cluster.Revert({
        model: this.loaded.cluster
      }).show();
    }
  },

  load: function(id) {
    this.collection
      .each(_.bind(function(model) {
        if(model.id === id) {
          model.save({ Loaded: true }, {
            success: _.bind(function() {
              this.render();
            },this)
          });
        } else {
          model.save({ Loaded: false }, {
            success: _.bind(function() {
              this.render();
            },this)
          });
        }
      }, this));
  },

  /* Check only one project is actually loaded, and, if not, 
   * unload all but the last and warn user.
   */
  checkProjects: function () {
    var loaded = this.collection.where({ Loaded: true });

    if(loaded.length > 1) {
      loaded.pop();
      _.map(loaded, function (project) {
        project.save({ Loaded: false }, {
          success: function() {
            $.growl('Conflict detected. <br/>' +
                    'Automatically unloaded Project "' +
                    project.get('Name') + '"<br/>',
                    { type: 'warning' });
          },
          error: function(args) {
            $.growl("Error unloading project: " +
                    project.get('Name') + "<br>" + args[0]);
          }
        });
      });
    }
  },
  
  onRender: function() {
    // Recent projecst list
    var list = new Projects({ collection: this.collection });
    this.listenTo(list, 'load', this.load);

    // remove registered callbacks
    if(this.loaded) this.stopListening(this.loaded);
    
    // find loaded project
    this.loaded = this.collection.findWhere({ Loaded: true });

    // 
    if(typeof this.loaded != 'undefined') {
      this.listenTo(this.loaded, 'cluster:change', this.initClusterView);

      if(this.loaded.clusterReady) {
        this.initClusterView(this.loaded.cluster);
      } else {
        this.listenToOnce(this.loaded,'cluster:ready', this.initClusterView);
      }

      this.current.show(new Project({
        model: this.loaded,
        detailed: true
      }));
    }
      
    this.recent.show(list);
  },

  initClusterView: function (cluster) {
    this.cluster.empty();

    this.ui.member.removeClass("hidden");

    switch(cluster.get('Status')) {
    case 'Activated':
      this.ui.member.addClass("disabled");
      this.ui.revert.removeClass("hidden");
      this.ui.commit.addClass("hidden");
      break;
    case 'Deactivated':
      this.ui.member.removeClass("disabled");
      this.ui.commit.removeClass("hidden");
      this.ui.revert.addClass("hidden");
      if(cluster.get('Members').length > 0) {
        this.ui.commit.removeClass("disabled");
      } else {
        this.ui.commit.addClass("disabled");
      }
      break;
    }

    if(cluster.get('Members').length > 0) {
      var view = new Cluster.Editor({ model: cluster });
      this.cluster.show(view);
    }
  }
});
