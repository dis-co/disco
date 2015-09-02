var Base    = require('../components/base');
var Monitor = require('../components/monitor');

module.exports = Base.LayoutView.extend({
  template: require('./templates/dashboard.hbs'),

  regions: {
    'processor': '#processor',
    'processes': '#processes',
    'memory':    '#memory',
    'network':   '#network'
  },

  onRender: function() {
    this.processor.show(new Monitor.Processor({
      collection: this.collection
    }));
    this.memory.show(new Monitor.Memory({
      collection: this.collection
    }));
    this.network.show(new Monitor.Network({
      collection: this.collection
    }));
    this.processes.show(new Monitor.Process({
      collection: this.collection
    }));
  }
});
