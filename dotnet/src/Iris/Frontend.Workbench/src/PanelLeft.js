import * as React from "react";
import css from "./Log.less";


export default class Log extends React.Component {
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
    this.state = { logs: initLogs };
  }

  render() {
    const logs = this.state.logs;
    return (
      <div className="iris-log">
        <div className="iris-draggable-handle">
          <span>LOG</span>
          <span className="iris-close">x</span>
        </div>
        <div>
          {this.state.logs.map((log,i) => <p key={i}>{log}</p>)}
        </div>
      </div>
    )
  }
}
