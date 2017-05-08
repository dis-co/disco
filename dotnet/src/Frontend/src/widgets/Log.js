import React, { Component } from 'react'
import css from "../../css/Log.less"
import { map } from "../Util.ts"

class View extends Component {
  constructor(props) {
    super(props);
  }

  componentDidMount() {
    this.disposable =
      this.props.global.subscribe("logs", () => {
        this.forceUpdate();
      });
  }

  componentWillUnmount() {
    if (this.disposable) {
      this.disposable.Dispose();
    }
  }

  render() {
    const logs = this.props.global.state.logs;
    return (
      <div className="iris-log">
        {map(logs, kv => <p key={kv[0]}>{kv[1]}</p>)}
      </div>
    )
  }
}

export default class Log {
  constructor() {
    this.view = View;
    this.name = "LOG";
    this.layout = {
      x: 0, y: 0,
      w: 3, h: 6,
      minW: 1, maxW: 10,
      minH: 1, maxH: 10
    }
  }
}