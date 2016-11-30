import * as React from "react";
import Panel from 'muicss/lib/react/panel';
import { removeNode } from "iris";

function map(iterable, f) {
  let ar = [];
  if (iterable != null)
    for (let x of iterable)
      ar.push(f(x));
  return ar;
}

export default function (props) {
  // console.log("Cluster props:", props)
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
          {map(props.info.state.Nodes, kv => {
            const node = kv[1];
            return (
              <tr key={kv[0].Fields[0]}>
                <td>{node.HostName}</td>
                <td>{node.IpAddr.Fields[0]}</td>
                <td>{node.Port}</td>
                <td>{node.State.ToString()}</td>
                <td>left</td>
                <td>Main, VideoPB, Show1</td>
                <td><a onClick={() => removeNode(props.info, kv[0])}>Remove</a></td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </Panel>
)};
