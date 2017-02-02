import * as React from "react";
import Panel from 'muicss/lib/react/panel';
import { subscribeToLogs } from "iris";

// If we need a table that loads cells on demand we can use fixed-data-table-2
// Check http://schrodinger.github.io/fixed-data-table-2/ for details

const initLogs = [
  // [0, "Do laboris fugiat cillum excepteur Lorem officia."],
  // [1, "Ullamco voluptate proident veniam adipisicing nisi esse dolore anim eiusmod."],
  // [2, "Aute nostrud consequat nulla commodo non."],
  // [3, "Non ad incididunt pariatur ullamco sit labore cupidatat aliqua ex consectetur ad dolore."],
  // [4, "Duis consectetur deserunt sint minim culpa aliquip."],
  // [5, "Aute excepteur excepteur quis sint officia incididunt aliquip cillum."],
  // [6, "Officia officia ad adipisicing non."],
  // [7, "Sit qui ullamco cillum Lorem sunt minim sit tempor."],
  // [8, "Laborum officia cillum enim ea sint adipisicing laborum nostrud velit Lorem non commodo dolore."],
]


let counter = initLogs + 1;
const maxLogLength = 100;
const logsToDeleteWhenMaxReached = 20;

export default class WidgetLog extends React.Component {
  static get layout() {
    return { x: 12, y: 0, w: 10, h: 8, minW: 7, maxW: 15, minH: 5, maxH: 20 };
  }

  constructor(props) {
    super(props);
    this.state = { logs: initLogs };
  }

  componentDidMount() {
    subscribeToLogs(this.props.context, log => {
      let logs = this.state.logs;
      if (logs.length > maxLogLength) {
        logs.splice(logs.length - logsToDeleteWhenMaxReached, logsToDeleteWhenMaxReached);
      }
      logs.splice(0, 0, [counter++, log]);
      this.setState({logs: logs});
    });
  }

  render() {
    const logs = this.state.logs;
    return (
      <Panel className="panel-cluster">
        <table
          className="mui-table mui-table--bordered"
          style={{ height: "100%" }} >
          <thead className="draggable-handle draggable-cursor">
            <tr>
              <th>Log Viewer</th>
            </tr>
          </thead>
          <tbody style={{
            display: "block",
            height: "100%",
            overflowY: "scroll"
          }}>
            {this.state.logs.map(kv => <tr key={kv[0]}><td>{kv[1]}</td></tr>)}
          </tbody>
        </table>
      </Panel>
    )
  }
}
