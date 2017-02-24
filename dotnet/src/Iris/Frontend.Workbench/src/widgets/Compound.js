import React, { Component } from 'react'
import css from "../../css/Compound.less"
import Spread from "./Spread"

export default class Compound extends Component {
  static get layout() {
    return {
      x: 0, y: 0,
      w: 5, h: 3,
      minW: 2, maxW: 10,
      minH: 1, maxH: 10
    };
  }

  constructor(props) {
    super(props);
  }

  render() {
    return (
      <div className="iris-compound">
        <div className="iris-draggable-handle">
          <span>COMPOUND</span>
          <span className="iris-close" onClick={() => {
            this.props.model.removeWidget(this.props.id);
          }}>x</span>
        </div>
        <div>
          <Spread model={this.props.model} />
          <Spread model={this.props.model} />
        </div>
      </div>
    )
  }
}
