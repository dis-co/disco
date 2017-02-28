import React, { Component } from 'react'

export default class Widget extends Component {
  constructor(props) {
    super(props);
  }

  render() {
    const Body = this.props.body;
    return (
      <div className="iris-widget">
        <div className="iris-draggable-handle">
          <span>{Body.name}</span>
          <span className="iris-close ui-icon ui-icon-close" onClick={() => {
            this.props.model.removeWidget(this.props.id);
          }}></span>
        </div>
        <Body model={this.props.model} />
      </div>
    )
  }
}
