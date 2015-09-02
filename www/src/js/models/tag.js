var Base = require('./base.js');

var Tag = Base.Model.extend({
  url: 'iris.tags',
  idAttribute: 'Tag'
});

var Tags = Base.Collection.extend({
  model: Tag,

  comparator: 'Tag',

  has: function(tag) {
    return this.contains({ Tag: tag });
  }
});

module.exports.Tag  = Tag;
module.exports.Tags = Tags;
