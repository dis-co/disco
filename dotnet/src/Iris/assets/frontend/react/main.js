define([], function () {
  "use strict";

  function _classCallCheck(instance, Constructor) {
    if (!(instance instanceof Constructor)) {
      throw new TypeError("Cannot call a class as a function");
    }
  }

  function _possibleConstructorReturn(self, call) {
    if (!self) {
      throw new ReferenceError("this hasn't been initialised - super() hasn't been called");
    }

    return call && (typeof call === "object" || typeof call === "function") ? call : self;
  }

  function _inherits(subClass, superClass) {
    if (typeof superClass !== "function" && superClass !== null) {
      throw new TypeError("Super expression must either be null or a function, not " + typeof superClass);
    }

    subClass.prototype = Object.create(superClass && superClass.prototype, {
      constructor: {
        value: subClass,
        enumerable: false,
        writable: true,
        configurable: true
      }
    });
    if (superClass) Object.setPrototypeOf ? Object.setPrototypeOf(subClass, superClass) : subClass.__proto__ = superClass;
  }

  var Form = function (_React$Component) {
    _inherits(Form, _React$Component);

    function Form(props) {
      _classCallCheck(this, Form);

      var _this = _possibleConstructorReturn(this, _React$Component.call(this, props));

      _this.state = { value: '' };
      return _this;
    }

    Form.prototype.handleChange = function handleChange(name, event) {
      var change = {};
      change[name] = event.target.value;
      this.setState(change);
    };

    Form.prototype.handleSubmit = function handleSubmit(event) {
      var _this2 = this;

      require([this.state.widgetName], function (com) {
        fetch("data/" + _this2.state.widgetData + ".json").then(function (res) {
          return res.json();
        }).then(function (json) {
          return _this2.setState({ widget: React.createElement(com.default, json) });
        }).catch(function (err) {
          return console.log(err);
        });
      });
    };

    Form.prototype.render = function render() {
      var el = this.state.widget;
      return React.createElement(
        "div",
        null,
        React.createElement(
          "div",
          null,
          React.createElement("input", { type: "text",
            placeholder: "Wiget name",
            value: this.state.widgetName,
            onChange: this.handleChange.bind(this, "widgetName") }),
          React.createElement("input", { type: "text",
            placeholder: "Data file",
            value: this.state.widgetData,
            onChange: this.handleChange.bind(this, "widgetData") }),
          React.createElement(
            "button",
            { onClick: this.handleSubmit.bind(this) },
            "Load widget"
          )
        ),
        el
      );
    };

    return Form;
  }(React.Component);

  ReactDOM.render(React.createElement(Form, null), document.getElementById('app'));
});