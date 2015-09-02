/**
 * <pre style="width:800px">
 *      ___           ___            ___         ___           ___
 *     /\  \         /\  \          /\  \       /\  \         /\  \
 *    /::\  \       /::\  \         \:\  \     /::\  \        \:\  \
 *   /:/\:\  \     /:/\:\  \    ___ /::\__\   /:/\:\  \        \:\  \
 *  /::\~\:\  \   /::\~\:\  \  /\  /:/\/__/  /:/  \:\  \       /::\  \
 * /:/\:\ \:\__\ /:/\:\ \:\__\ \:\/:/  /\   /:/__/ \:\__\     /:/\:\__\
 * \/__\:\/:/  / \/_|::\/:/  /  \::/  /  e--\:\  \  \/__/    /:/  \/__/
 *      \::/  /     |:|::/  /\ / \/__/       \:\  \     \   /:/  /
 *       \/__/      |:|\/__/  o               \:\  \     \/ \/__/
 *                  |:|  |                     \:\__\    *
 *                   \|__|                      \/__/
 * </pre>
 *
 * Metadata for the currently opened Project in _VVVV_.
 */
var _       = require('underscore');
var Base    = require('./base.js');
var Cluster = require('./cluster.js');

var Project = Base.Model.extend({
  idAttribute: '_id',

  url: 'iris.projects',

  defaults: {
    Type: 'Project'
  },

  clusterReady: false,

  initialize: function (options) {
    if(typeof this.cluster  ===   'undefined' &&
       typeof this.get('_id')  != 'undefined' &&
       typeof this.get('_rev') != 'undefined') {
      /* load the cluster configuration for this project */
      this.loadCluster();
    }
  },

  createCluster: function (done) {
    if(typeof this.get('_id')  === 'undefined' ||
       typeof this.get('_rev') === 'undefined' ||
       typeof this.cluster     !=  'undefined')
      throw("Model is has not been saved yet or your're messing around'. Aborting.");

    var cluster = new Cluster.Model({
      Name: this.get('Name') + ' Cluster',
      Status: Cluster.Status.Deactivated
    });

    cluster.save({}, {
      success: _.bind(function() {
        this.cluster = cluster;
        this.clusterReady = true;

        cluster.on('change', _.bind(function () {
          this.trigger('cluster:change', cluster);
        },this));

        this.save({
          ClusterId: cluster.id
        }, {
          success: function () {
            done(null, cluster);
          },
          error: function (err) {
            done(err, null);
          }
        });
      }, this),
      error: _.bind(function() {
        console.log('error saving ccluster: ', arguments);
        $.growl("Error saving cluster. ", { type: 'danger' });
      }, this)
    });
  },

  loadCluster: function () {
    var cluster = new Cluster.Model({
      _id: this.get('ClusterId')
    });

    cluster.fetch({
      success: _.bind(function() {
        this.cluster = cluster;
        this.clusterReady = true;

        cluster.on('change', _.bind(function () {
          this.trigger('cluster:change', cluster);
        },this));

        this.trigger('cluster:ready', cluster);
      },this),
      error: function(args) {
        this.trigger('cluster:error', args);
        $.growl('Error loading cluster config.<br>' + args[0], {
          type: 'danger'
        });
      }
    });
  }
});

var Projects = Base.Collection.extend({
  url: 'iris.projects',
  model: Project,

  comparator: 'Name',

  active: function() {
    var project = this.findWhere({ Loaded: true });
    return project;
  }
});

module.exports.Model      = Project;
module.exports.Collection = Projects;
