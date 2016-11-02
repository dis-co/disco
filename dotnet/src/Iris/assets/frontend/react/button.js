define(["exports"], function (exports) {
    "use strict";

    exports.__esModule = true;

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

    var Button = function (_React$Component) {
        _inherits(Button, _React$Component);

        function Button(props) {
            _classCallCheck(this, Button);

            var _this = _possibleConstructorReturn(this, _React$Component.call(this, props));

            _this.props = props;
            return _this;
        }

        Button.prototype.render = function render() {
            var buttonStyle = {
                background: this.props.background || "yellow"
            };
            return React.createElement(
                "button",
                { style: buttonStyle },
                "Hello World!"
            );
        };

        return Button;
    }(React.Component);

    exports.default = Button;
});