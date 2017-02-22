import * as React from "react";
import css from "./Log.less";

const initLogs = [
  "[14:12:11] Do laboris fugiat cillum excepteur Lorem officia.",
  "[14:12:11] Ullamco voluptate proident veniam adipisicing nisi esse dolore anim eiusmod.",
  "[14:12:11] Aute nostrud consequat nulla commodo non.",
  "[14:12:11] Non ad incididunt pariatur ullamco sit labore cupidatat aliqua ex consectetur ad dolore.",
  "[14:12:11] Duis consectetur deserunt sint minim culpa aliquip.",
  "[14:12:11] Aute excepteur excepteur quis sint officia incididunt aliquip cillum.",
  "[14:12:11] Officia officia ad adipisicing non.",
  "[14:12:11] Sit qui ullamco cillum Lorem sunt minim sit tempor.",
  "[14:12:11] Laborum officia cillum enim ea sint adipisicing laborum nostrud velit Lorem non commodo dolore.",
]

export default class Log extends React.Component {
  static get layout() {
    return {
      x: 0, y: 0,
      w: 5, h: 8,
      minW: 3, maxW: 10,
      minH: 3, maxH: 15
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
