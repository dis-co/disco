import * as React from "react";
import Panel from 'muicss/lib/react/panel';
import { triggerDragEvent, getDiscoveredServices, toString } from "iris";
import Draggable from 'react-draggable';

export default class WidgetDiscovery extends React.Component {
  static get layout() {
    return {x: 8, y: 0, w: 3, h: 7, minW: 2, maxW: 4, minH: 2, maxH: 7 };
  }

  constructor(props) {
    super(props);
    this.state = { services: [] };
    this.controlledPositions = new Map();
  }

  componentDidMount() {
    getDiscoveredServices()
      .then(services => {
        debugger;
        this.setState({ services })
      })
  }

  renderService(service) {
    var id = toString(service.Id)
    var info = {
      tag: "service",
      id: id,
      host: service.Hostname,
      ip: toString(service.IpAddr),
      port: toString(service.Port),
      wsPort: toString(service.WsPort),
      gitPort: toString(service.GitPort),
      apiPort: toString(service.ApiPort)
    }
    var props = {}, pos = this.controlledPositions.get(id);
    if (pos != null && pos.pending) {
      props = {position: {x: pos.x, y: pos.y}}
      pos.pending = false;
    }

    return (<tr key={id}>
      <td className="draggable-cursor">
        <Draggable {...props}
          onThisDragOver={() => {debugger;}}
          onStart={(e,{x,y}) => {
            this.controlledPositions.set(id, {x, y, pending: false});
          }}
          onDrag={(e,pos) => {
            triggerDragEvent("move", info, e.clientX, e.clientY);
          }}
          onStop={(e,{x,y}) => {
            triggerDragEvent("stop", info, e.clientX, e.clientY);
            var pos = this.controlledPositions.get(id);
            pos.pending = true;
            this.forceUpdate();
          }}
        >
          <div style={{background:"red"}} >{id.substr(0, 4) + "..."}</div>
        </Draggable>
      </td>
    </tr>)

  }

  render() {
    return (
      <Panel className="panel-cluster" >
        <table className="mui-table mui-table--bordered">
          <thead className="draggable-handle draggable-cursor">
            <tr><td>Services</td></tr>
          </thead>
          <tbody>
            {this.state.services.map(x => this.renderService(x))}
          </tbody>
        </table>
      </Panel>
    )
  }
}
