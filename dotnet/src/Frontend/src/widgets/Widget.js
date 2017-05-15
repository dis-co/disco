import React, { Component } from 'react'

export default class Widget extends Component {
  constructor(props) {
    super(props);
  }

  render() {
    return (
      <div className="iris-widget">
        <div className="iris-draggable-handle">
          <span>{this.props.model.name}</span>
          <span className="ui-icon ui-icon-copy" onClick={() => {
            this.props.global.addTab(this.props.model, this.props.id);
            this.props.global.removeWidget(this.props.id);
          }}></span>
          <span className="ui-icon ui-icon-close" onClick={() => {
            this.props.global.removeWidget(this.props.id);
          }}></span>
        </div>
        {this.props.children}
      </div>
    )
  }
}
