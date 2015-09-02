var _    = require('underscore');
var Base = require('../components/base');

module.exports = Base.ItemView.extend({
  template: require('./templates/confirm.hbs'),

  className: 'modal fade',

  events: {
    'click button.delete':  'delete',
    'click button.dismiss': 'close',
    'hide.bs.modal': 'closed'
  },

  ui: {
    'delete':  'button.delete',
    'dismiss': 'button.dismiss',
    'warning': 'span.warning',
    'status':  '.status',
    'error':   'span.error',
    'spinner': 'img.spinner'
  },

  delete: function() {
    this.ui.warning.hide();
    this.ui.status.show();
    this.ui.spinner.show();

    this.disable();

    if(this.model.cluster) {
      this.model.cluster.destroy({
        success: _.bind(this.destroyModel, this),
        error: _.bind(function (child, data) {
          this.ui.spinner.addClass('hidden');
          this.ui.error.append(data['args'][0]);
          this.ui.error.removeClass('hidden');
          this.ui.dismiss.attr('disabled', null);
        }, this)
      });
    } else {
      this.destroyModel();
    }
  },

  destroyModel: function (arg) {
    this.model.destroy({
      success: _.bind(function() {
        this.ui.spinner.addClass('hidden');
        this.model.trigger('rerender');
        this.close();
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
    this.ui.delete.attr('disabled', null);
  },

  disable: function () {
    this.ui.dismiss.attr('disabled','disabled');
    this.ui.delete.attr('disabled','disabled');
  },

  closed: function() {
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
