import * as React from "react";
import Panel from 'muicss/lib/react/panel';

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
          </tr>
        </thead>
        <tbody>
          {map(props.nodes, kv => {
            const node = kv[1];
            return (
              <tr key={kv[0].Fields[0]}>
                <td>{node.HostName}</td>
                <td>{node.IpAddr.Fields[0]}</td>
                <td>{node.Port}</td>
                <td>{node.State.Case}</td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </Panel>
)};
