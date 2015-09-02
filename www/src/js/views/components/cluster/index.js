var _        = require('underscore');
var Backbone = require('backbone');
var Base     = require('../base');
var Cluster  = require('../../../models/cluster.js');

var Member = Base.ItemView.extend({
  tagName: 'li',
  template: require('./templates/member.hbs'),

  ui: {
    delete: 'button.delete'
  },

  initialize: function (options) {
    this.active = options.active;
  },

  onRender: function () {
    if(this.active) {
      this.ui.delete.removeClass('disabled');
    } else {
      this.ui.delete.addClass('disabled');
    }
  }
});

var MemberList = Base.CollectionView.extend({
  tagName: 'ul',
  className: 'list-unstyled',
  childView: Member,

  initialize: function (options) {
    this.active = options.active;
  },
  
  buildChildView: function(child, ChildViewClass, childViewOptions) {
    var options = _.extend({
      model:  child,
      active: this.active
    }, childViewOptions);
    return new ChildViewClass(options);
  }
});

module.exports.Modal = Base.ItemView.extend({
  template: require('./templates/edit.hbs'),

  className: 'modal fade',

  events: {
    'click button.save': 'save',
    'click button.dismiss': 'close',
    'hide.bs.modal': 'closed'
  },

  ui: {
    'ip':       'input[name=ip]',
    'hostname': 'input[name=hostname]'
  },

  save: function() {
    var current = this.model.get('Members');

    var member = {
      HostName: this.ui.hostname.val(),
      IP: this.ui.ip.val()
    };

    var exist = _.any(current, function(mem) {
      return mem.IP === member.IP;
    });

    if(!exist) {
      current.push(member);
      this.model.save({
        Members: current
      }, {
        success: _.bind(function() {
          this.close();
        },this)
      });
    }
  },

  closed: function() {
    this.model = null; // important, otherwise model's handlers gets
                       // destroyed as well! WTF?
    this.destroy();
  },

  close: function() {
    $('#modals').children().first().modal('hide');
  },

  show: function() {
    this.render();
    $('#modals').html(this.$el);
    $('#modals')
      .children()
      .first()
      .modal({ backdrop: 'static', keyboard: false });
  }
});

module.exports.Commit = Base.ItemView.extend({
  template: require('./templates/commit.hbs'),

  className: 'modal fade',

  events: {
    'click button.commit': 'commit',
    'click button.dismiss': 'close',
    'hide.bs.modal': 'closed'
  },

  ui: {
    'commit':  'button.commit',
    'dismiss': 'button.dismiss',
    'warning': 'span.warning',
    'status':  '.status',
    'spinner': 'img.spinner',
    'error':   'span.error',
    'success': 'span.success'
  },

  commit: function() {
    this.ui.warning.hide();
    this.ui.status.show();
    this.ui.spinner.show();

    this.disable();

    this.model.save({
      Status: Cluster.Status.Activate
    }, {
      success: _.bind(function() {
        this.ui.spinner.addClass('hidden');
        this.ui.success.removeClass('hidden');
        this.model.trigger('rerender');
        setTimeout(_.bind(function() {
          this.close();
        },this), 1000);
      },this),
      error: _.bind(function(child, data) {
        this.ui.spinner.addClass('hidden');
        this.ui.error.append(data['args'][0]);
        this.ui.error.removeClass('hidden');
        this.ui.dismiss.attr('disabled', null);
      },this)
    });
  },

  enable: function () {
    this.ui.dismiss.attr('disabled', null);
    this.ui.commit.attr('disabled', null);
  },

  disable: function () {
    this.ui.dismiss.attr('disabled','disabled');
    this.ui.commit.attr('disabled','disabled');
  },

  closed: function() {
    this.model = null; // important, otherwise model gets destroyed as well!
    this.destroy();
  },

  close: function() {
    $('#modals').children().first().modal('hide');
  },

  show: function() {
    this.render();
    this.ui.status.hide();
    $('#modals').html(this.$el);
    $('#modals')
      .children()
      .first()
      .modal({ backdrop: 'static', keyboard: false });
  }
});

module.exports.Revert = Base.ItemView.extend({
  template: require('./templates/revert.hbs'),

  className: 'modal fade',

  events: {
    'click button.revert': 'revert',
    'click button.dismiss': 'close',
    'hide.bs.modal': 'closed'
  },

  ui: {
    'revert':  'button.revert',
    'dismiss': 'button.dismiss',
    'warning': 'span.warning',
    'status':  '.status',
    'spinner': 'img.spinner',
    'error':   'span.error',
    'success': 'span.success'
  },

  revert: function() {
    this.ui.warning.hide();
    this.ui.status.show();
    this.ui.spinner.show();

    this.disable();

    this.model.save({
      Status: Cluster.Status.Deactivate
    }, {
      success: _.bind(function() {
        this.ui.spinner.addClass('hidden');
        this.ui.success.removeClass('hidden');
        this.model.trigger('rerender');
        setTimeout(_.bind(function() {
          this.close();
        },this), 1000);
      },this),
      error: _.bind(function(child, data) {
        this.ui.spinner.addClass('hidden');
        this.ui.error.append(data['args'][0]);
        this.ui.error.removeClass('hidden');
        this.ui.dismiss.attr('disabled', null);
      },this)
    });
  },

  enable: function () {
    this.ui.dismiss.attr('disabled', null);
    this.ui.revert.attr('disabled', null);
  },

  disable: function () {
    this.ui.dismiss.attr('disabled','disabled');
    this.ui.revert.attr('disabled','disabled');
  },

  closed: function() {
    this.model = null; // important, otherwise model gets destroyed as well!
    this.destroy();
  },

  close: function() {
    this.model = null;
    $('#modals').children().first().modal('hide');
  },

  show: function() {
    this.render();
    this.ui.status.hide();
    $('#modals').html(this.$el);
    $('#modals')
      .children()
      .first()
      .modal({ backdrop: 'static', keyboard: false });
  }
});

module.exports.Editor = Base.LayoutView.extend({
  template: require('./templates/editor.hbs'),

  events: {
    'click button.delete': 'delete'
  },

  regions: {
    'members': '.members'
  },

  delete: function(event) {
    var $el = $(event.target);

    if($el.hasClass('disabled') || $el.parent().hasClass('disabled')) return;
    
    var ip = $(event.target).attr('data-target-ip');
    var current = this.model.get('Members');

    if(!$el.hasClass('delete')) {
      ip = $el.parent().attr('data-target-ip');
    } 

    this.model.save({
      Members: current.filter(function(item) {
        return item.IP != ip;
      })
    }, {
      success: _.bind(function() {
        this.model.trigger('rerender');
      },this)
    });
  },
  
  onRender: function() {
    this.cluster = new Backbone.Collection(this.model.get('Members'));
    if(this.cluster.size() > 0) {
      this.members.show(new MemberList({
        active: (this.model.get('Status') === Cluster.Status.Deactivated),
        collection: this.cluster
      }));
    }
  }
});
