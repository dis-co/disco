import * as React from "react";
import Panel from 'muicss/lib/react/panel';
import { showModal } from '../App';
import { removeMember, addMember, subscribeToDrags } from "iris";
import ADD_NODE from "../modals/AddNode";
import { map, touchesElement } from "../Util.ts";

export default class WidgetCluster extends React.Component {
  static get layout() {
    return { x: 0, y: 0, w: 12, h: 12, minW: 3, maxW: 15, minH: 3, maxH: 15 };
  }

  constructor(props) {
    super(props);
    this.el = null;
  }

  componentDidMount() {
    subscribeToDrags(ev => {
      if (this.el != null && ev.value.tag === "service") {
        if (touchesElement(this.el, ev.x, ev.y)) {
          switch (ev.type) {
            case "move":
              this.el.parentNode.classList.add("highlight-blue");
              return;
            case "stop":
              var x = ev.value;
              console.log("Add member with info:", x);
              addMember(this.props.info, x.id, x.host, x.ip, x.port, x.wsPort, x.gitPort, x.apiPort);
          }
        }
        this.el.parentNode.classList.remove("highlight-blue")
      }
    });
  }

  render() {
    return (
      <Panel
        className="panel-cluster"
        style={{overflowX: "scroll"}} >
        <table
          className="mui-table mui-table--bordered"
          ref={el => this.el = el} >
          <thead className="draggable-handle draggable-cursor">
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
            {map(this.props.info.state.Project.Config.Cluster.Members, kv => {
              const node = kv[1];
              return (
                <tr key={kv[0].Fields[0]}>
                  <td>{node.HostName}</td>
                  <td>{node.IpAddr.Fields[0]}</td>
                  <td>{node.Port}</td>
                  <td>{node.State.ToString()}</td>
                  <td>left</td>
                  <td>Main, VideoPB, Show1</td>
                  <td><a onClick={() => removeMember(this.props.info, kv[0])}>Remove</a></td>
                </tr>
              );
            })}
          </tbody>
          <tfoot>
            <tr><td><a onClick={() => showModal(ADD_NODE)}>Add node</a></td></tr>
          </tfoot>
        </table>
      </Panel>
    )
  }
}
