var _          = require('underscore');
var async      = require('async');
var Marionette = require('backbone.marionette');

var iris       = require('./iris.js');
var views      = require('../views');
var models     = require('../models');

var withActiveProject = function(callback) {
  var projects = new models.Project.Collection();
  projects.fetch({
    reset: true,
    success: function() {
      var current = projects.active();
      if(typeof current === 'undefined') {
        callback('no active project. please select/create one!');
      } else {
        callback(null, current);
      }
    },
    error: function(err) {
      callback(err);
    }
  });
};

var get = function(thing) {
  return function(cb) {
    thing.fetch({
      success: function(result) {
        cb(null, result);
      },
      error: function(err) {
        cb(err, null);
      }
    });
  };
};

var dynamicPatchView = function(view, project, ctx) {
  var ids = project.get('Views')[view] || [];
  return _.bind(function() {
    var cues = new models.Cue.Collection({
      project: project.get('Database')
    });

    var patches = new models.Patch.Collection();
    patches.fetch();

    setTimeout(_.bind(function() {
      async.parallel({
        cues: get(cues)
      }, _.bind(function(err, result) {
        result.patches = patches;
        result.ids = ids;
        this.iris.main.empty();
        this.iris.main.show(new views.Patch.Editor(result));
      },this));
    },this), 400);
  },ctx)
};

/**
 * __Router__
 */
module.exports = Marionette.AppRouter.extend({
  initialize: function(projects) {
    this.projects = projects;
    this.iris = require('./iris.js');

    var views;
    var active = this.projects.findWhere({ Loaded: true });
    if(active) views = active.get('Views');

    // if there are indeed dynamic views defined
    if(active && views && !(Object.keys(views).length === 0)) {
      _.each(Object.keys(views), _.bind(function(name) {
        this.route(name, name, dynamicPatchView(name, active, this));
      },this));
    }
  },

  routes: {
    ''          : 'index',
    'dashboard' : 'dashboard',
    'project'   : 'projects',
    'patches'   : 'patches',
    'cuelists'  : 'cuelists',
    'player'    : 'player',
    'logs'      : 'logs'
  },

  // __Index__
  index: function(options) {
    this.iris.router.navigate('project', { trigger: true });
  },

  // __Dashboard__
  dashboard: function(options) {
    this.iris.router.navigate('project', { trigger: true });
  },

  // __Project View__
  projects: function(options) {
    var projects = new models.Project.Collection();

    var clusters = new models.Cluster.Collection();
    clusters.fetch({ reset: true });

    projects.fetch({
      reset: true,
      success: _.bind(function() {
        this.iris.main.empty();
        this.iris.main.show(new views.Project.View({
          collection: projects
        }));
      }, this)
    });
  },

  // __Patches__
  patches: function(options) {
    withActiveProject(_.bind(function(err, project) {
      if(err) {
        $.growl(err, { type: 'danger' });
        return;
      }

      var cues = new models.Cue.Collection({
        project: project.get('Database')
      });

      var patches = new models.Patch.Collection();
      patches.fetch();

      setTimeout(_.bind(function() {
        async.parallel({
          cues: get(cues)
        }, _.bind(function(err, result) {
          result.patches = patches;
          this.iris.main.empty();
          this.iris.main.show(new views.Patch.Editor(result));
        },this));
      },this), 100);
    },this));
  },

  // __Cue Lists__
  cuelists: function() {
    withActiveProject(_.bind(function(err, project) {
      if(err) {
        $.growl(err, { type: 'danger' });
        return;
      }

      var cues = new models.Cue.Collection({
        project: project.get('Database')
      });

      var cuelists = new models.CueList.Collection({
        project: project.get('Database')
      });

      async.parallel({
        cues:     get(cues),
        cuelists: get(cuelists)
      }, _.bind(function(err, result) {
        this.iris.main.empty();
        this.iris.main.show(new views.CueLists.Editor(result));
      }, this));
    }, this));
  },

  player: function() {
    withActiveProject(_.bind(function(err, project) {
      if(err) {
        $.growl(err, { type: 'danger' });
        return;
      }

      var cues = new models.Cue.Collection({
        project: project.get('Database')
      });

      var cuelists = new models.CueList.Collection({
        project: project.get('Database')
      });

      async.parallel({
        cues:     get(cues),
        cuelists: get(cuelists)
      }, _.bind(function(err, result) {
        this.iris.main.empty();
        this.iris.main.show(new views.Player(result));
      }, this));
    }, this));
  },

  logs: function() {
    this.iris.main.empty();
    this.iris.main.show(new views.LogView({
      collection: new models.Logs.Collection()
    }));
  }
});
