import React, { Component } from 'react'
import Spread from "./Spread"

export default class Compound extends Component {
  static get name() {
    return "COMPOUND";
  }

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
        <Spread model={this.props.model} />
        <Spread model={this.props.model} />
      </div>
    )
  }
}
