import * as React from "react";

export default class DropdownMenu extends React.Component {
  constructor(props) {
    super(props);
    this.state = {};
  }

  renderDropdown() {
    if (!this.state.open) {
      return null;
    }
    return (
      <ul className="iris-dropdown-options">
        {this.props.options.map((option, k) =>
          <li key={k} onClick={ev=> {
            option.action();
            this.toggle(false);
          }}>{option.label()}</li>
        )}
      </ul>
    )
  }

  toggle(value) {
    if (value !== void 0) {
      if (value !== this.state.open)
        this.setState({open: value});
    }
    else {
      this.setState({open: !this.state.open});
    }
  }

  render() {
    var title = this.props.title || "Menu";
    return (
      <div className="iris-dropdown"
        onMouseLeave={ev => this.toggle(false)}>
        <div
          onClick={ev => this.toggle()}>
          <span>{title} </span><span className="ui-icon ui-icon-triangle-1-s" />
        </div>
          {this.renderDropdown()}
      </div>
    )
  }
}
