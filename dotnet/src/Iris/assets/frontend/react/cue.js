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

    var Cue = function (_React$Component) {
        _inherits(Cue, _React$Component);

        function Cue(props) {
            _classCallCheck(this, Cue);

            var _this = _possibleConstructorReturn(this, _React$Component.call(this, props));

            _this.props = props;
            return _this;
        }

        Cue.prototype.render = function render() {
            var _this2 = this;

            var cueStyle = {
                border: "2px solid #888",
                minHeight: "30px",
                minWidth: "80px"
            };
            var ulStyle = {
                listStyle: "none"
            };
            return React.createElement(
                "div",
                { "class": "cue",
                    style: cueStyle,
                    onClick: function onClick(ev) {
                        return window.alert(_this2.props.message);
                    } },
                React.createElement(
                    "ul",
                    { style: ulStyle },
                    React.createElement(
                        "li",
                        null,
                        React.createElement(
                            "strong",
                            null,
                            "Id:"
                        ),
                        React.createElement(
                            "div",
                            { id: "id" },
                            this.props.id
                        )
                    ),
                    React.createElement(
                        "li",
                        null,
                        React.createElement(
                            "strong",
                            null,
                            "Name:"
                        ),
                        React.createElement(
                            "div",
                            { id: "name" },
                            this.props.name
                        )
                    ),
                    React.createElement(
                        "li",
                        null,
                        React.createElement(
                            "button",
                            { id: "play" },
                            "Play"
                        )
                    )
                )
            );
        };

        return Cue;
    }(React.Component);

    exports.default = Cue;
});