import React, { Component } from 'react'
import Spread from "./Spread"
import IOBox from "./IOBox"
import domtoimage from "dom-to-image"
import { touchesElement, map } from "../Util.ts"

class View extends Component {
  constructor(props) {
    super(props);
  }

  componentDidMount() {
    this.disposable =
      this.props.global.subscribe("clock", () => {
        this.forceUpdate();
      });
  }

  componentWillUnmount() {
    if (this.disposable) {
      this.disposable.dispose();
    }
  }

  render() {
    return (
      <div>
        <span>{this.props.global.state.clock}</span>
      </div>
    )
  }
}

export default class CueList {
  constructor() {
    this.view = View;
    this.name = "Cue List";
    this.layout = {
      x: 0, y: 0,
      w: 8, h: 5,
      minW: 2, maxW: 10,
      minH: 1, maxH: 10
    };
  }
}
