var Base = require('./base.js');

var Status = {
  Deactivated: 'Deactivated',
  Deactivate:  'Deactivate',
  Activate:    'Activate',
  Activated:   'Activated'
};

var Cluster = Base.Model.extend({
  idAttribute: '_id',

  url: 'iris.cluster',

  defaults: {
    Members: []
  } 
});

var Clusters = Base.Collection.extend({
  url: 'iris.cluster',

  model: Cluster
});

module.exports.Status = Status;
module.exports.Model = Cluster;
module.exports.Collection = Clusters;
