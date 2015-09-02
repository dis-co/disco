var Base = require('../components/base');
var Channel = require('../../lib/channels.js');
var _ = require('underscore');

module.exports = Channel.infect(Base.ItemView).extend({
  template: require('./templates/navigation.hbs'),

  templateHelpers: function() {
    return {
      views: (this.project && this.project.get('Views'))
        ? Object.keys(this.project.get('Views'))
        : []
    }
  },

  className: 'container-fluid',

  events: {
    'click a.navbar-brand': 'dashboard',
    'click li.navitem': 'navigate'
  },

  ui: {
    menu: '#iris-menu'
  },

  actions: {
    'event/transport/connected': function() {
      this.$el.removeClass('disconnected');
      $.growl('Connected!', { type: 'success' });
    },
    'event/transport/disconnected': function() {
      this.$el.addClass('disconnected');

      $.growl('Disconnected. Please reload this page.', {
        type: 'danger'
      });
    }
  },

  initialize: function(options) {
    //var status = Radio.channel('transport');
    this.projects = options.projects;
    this.listenTo(this.projects, 'sync', _.bind(function() {
      this.project = this.projects.findWhere({ Loaded: true });
      if(this.project != null) {
        this.render();
      }
    },this));
    this.router = options.router;
  },

  dashboard: function(event) {
    event.preventDefault();
    this.$el.find('li.active').removeClass('active');
    this.router.navigate('/dashboard', { trigger: true });
  },

  navigate: function(event) {
    event.preventDefault();
    var $el = $(event.currentTarget);
    var target = $el.attr('data-target');

    this.$el.find('li.active').removeClass('active');
    $el.addClass('active');

    this.router.navigate('/' + target, { trigger: true });
    this.ui.menu.collapse('hide');
    window.location.reload();
  }
});
