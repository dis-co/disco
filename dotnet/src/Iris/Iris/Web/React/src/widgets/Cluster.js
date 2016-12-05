import * as React from "react";
import Panel from 'muicss/lib/react/panel';
import { showModal } from '../Main';
import { removeNode } from "iris";
import { MODALS } from "../Constants";
import { map } from "../Util";

export default class WidgetCluster extends React.Component {
  constructor(props) {
    super(props);
  }

  render() {
    return (
      <Panel className="panel-cluster">
        <table className="mui-table mui-table--bordered">
          <thead>
            <tr>
              <th>Host</th>
              <th>IP</th>
              <th>Port</th>
              <th>State</th>
              <th>Role</th>
              <th>Tags</th>
            </tr>
          </thead>
          <tbody>
            {map(this.props.info.state.Nodes, kv => {
              const node = kv[1];
              return (
                <tr key={kv[0].Fields[0]}>
                  <td>{node.HostName}</td>
                  <td>{node.IpAddr.Fields[0]}</td>
                  <td>{node.Port}</td>
                  <td>{node.State.ToString()}</td>
                  <td>left</td>
                  <td>Main, VideoPB, Show1</td>
                  <td><a onClick={() => removeNode(this.props.info, kv[0])}>Remove</a></td>
                </tr>
              );
            })}
          </tbody>
        </table>
        <a onClick={() => showModal(MODALS.ADD_NODE)}>Add node</a>
      </Panel>
    )
  }
}
