import React, { Component } from 'react'
import css from "../../css/Log.less"
import { map } from "../Util.ts"

export default class Log extends Component {
  static get name() {
    return "LOG";
  }

  static get layout() {
    return {
      x: 0, y: 0,
      w: 3, h: 6,
      minW: 1, maxW: 10,
      minH: 1, maxH: 10
    };
  }

  constructor(props) {
    super(props);
    this.state = { };
  }

  componentDidMount() {
    this.disposable =
      this.props.model.subscribe("logs", logs => {
        console.log("Updating log ", this.props.id);
        this.setState({ logs });
      });
  }

  componentWillUnmount() {
    if (this.disposable) {
      this.disposable.dispose();
      console.log("Log disposed");
    }
  }

  render() {
    const logs = this.state.logs || this.props.model.state.logs;
    return (
      <div className="iris-log">
        {map(logs, kv => <p key={kv[0]}>{kv[1]}</p>)}
      </div>
    )
  }
}
