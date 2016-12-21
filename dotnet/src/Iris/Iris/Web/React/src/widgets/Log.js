import * as React from "react";
import Panel from 'muicss/lib/react/panel';
import { subscribeToLogs } from "iris";

let counter = 0;

export default class WidgetLog extends React.Component {
  constructor(props) {
    super(props);
    this.state = { logs: [] };
  }

  componentDidMount() {
    subscribeToLogs(this.props.context, log => {
      this.state.logs.splice(0, 0, [counter++, log]);
      this.setState({logs: this.state.logs});
    });
  }

  render() {
    return (
      <Panel className="panel-cluster">
      <h2>Log Viewer</h2>
        <table className="mui-table mui-table--bordered">
          <tbody>
            {this.state.logs.map(kv => <tr key={kv[0]}><td>{kv[1]}</td></tr>)}
          </tbody>
        </table>
      </Panel>
    )
  }
}
