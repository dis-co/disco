import React, { Component } from 'react'
import { map } from "../Util.ts"

class View extends Component {
  constructor(props) {
    super(props);
    this.state = { logs: [] };
  }

  componentDidMount() {
    this.disposable =
      this.props.global.subscribe("logs", logs => {
        this.setState({logs});
      });
  }

  componentWillUnmount() {
    if (this.disposable) {
      this.disposable.dispose();
    }
  }

  render() {
    return (
      <div className="iris-log">
        {map(this.state.logs, (log, i) => <p key={i}>{log}</p>)}
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