import * as React from "react";
import Panel from 'muicss/lib/react/panel';
import { triggerDragEvent, createMemberInfo } from "iris";
import Draggable from 'react-draggable';

export default class WidgetDiscovery extends React.Component {
  static get layout() {
    return {x: 8, y: 0, w: 3, h: 7, minW: 2, maxW: 4, minH: 2, maxH: 7 };
  }

  constructor(props) {
    super(props);
    this.controlledPositions = new Map();
  }

  renderRow(i) {
    var _info = createMemberInfo()
    var info = {
      tag: "service",
      id: _info[0],
      host: _info[1],
      ip: _info[2],
      port: _info[3],
      wsPort: _info[4],
      gitPort: _info[5],
      apiPort: _info[6]
    }
    var props = {}, pos = this.controlledPositions.get(i);
    if (pos != null && pos.pending) {
      props = {position: {x: pos.x, y: pos.y}}
      pos.pending = false;
    }

    return (<tr key={i}>
      <td className="draggable-cursor">
        <Draggable {...props}
          onThisDragOver={() => {debugger;}}
          onStart={(e,{x,y}) => {
            this.controlledPositions.set(i,{x, y, pending: false});
          }}
          onDrag={(e,pos) => {
            triggerDragEvent("move", info, e.clientX, e.clientY);
          }}
          onStop={(e,{x,y}) => {
            triggerDragEvent("stop", info, e.clientX, e.clientY);
            var pos = this.controlledPositions.get(i);
            pos.pending = true;
            this.forceUpdate();
          }}
        >
          <div style={{background:"red"}} >Service {i}</div>
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
            {[1,2,3,4,5].map(x => this.renderRow(x))}
          </tbody>
        </table>
      </Panel>
    )
  }
}
