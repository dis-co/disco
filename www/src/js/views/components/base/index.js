var Marionette = require('backbone.marionette');

var cleanup = function() {
  if(this.model)      this.model.stopListening();
  if(this.collection) this.collection.stopListening();
};

/**
 * __ ItemView Cleanup __ 
 *
 * add a handler, which cleans up all event listeners/handlers
 * on models and collections to prevent memory leaks.
 */
module.exports.ItemView = Marionette.ItemView.extend({
  remove: function() {
    cleanup.call(this);
    Marionette.ItemView.prototype.remove.apply(this, arguments);
  }
});

/**
 * __ CollectionView Cleanup __ 
 *
 * add a handler, which cleans up all event listeners/handlers
 * on models and collections to prevent memory leaks.
 */
module.exports.CollectionView = Marionette.CollectionView.extend({
  remove: function() {
    cleanup.call(this);
    Marionette.CollectionView.prototype.remove.apply(this, arguments);
  }
});

/**
 * __ CompositeView Cleanup __ 
 *
 * add a handler, which cleans up all event listeners/handlers
 * on models and collections to prevent memory leaks.
 */
module.exports.CompositeView = Marionette.CompositeView.extend({
  remove: function() {
    cleanup.call(this);
    Marionette.CompositeView.prototype.remove.apply(this, arguments);
  }
});

/**
 * __ LayoutView Cleanup __ 
 *
 * add a handler, which cleans up all event listeners/handlers
 * on models and collections to prevent memory leaks.
 */
module.exports.LayoutView = Marionette.LayoutView.extend({
  remove: function() {
    cleanup.call(this);
    Marionette.LayoutView.prototype.remove.apply(this, arguments);
  }
});
