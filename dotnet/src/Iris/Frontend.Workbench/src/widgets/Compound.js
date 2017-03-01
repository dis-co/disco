import React, { Component } from 'react'
import Spread from "./Spread"

class View extends Component {
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

export default class Manager {
  constructor() {
    this.view = View;
    this.name = "COMPOUND";
    this.layout = {
      x: 0, y: 0,
      w: 5, h: 3,
      minW: 2, maxW: 10,
      minH: 1, maxH: 10
    };
  }
}
