import * as React from "react";
import LeftPanel from "./LeftPanel";
import CenterPanel from "./CenterPanel";
import RightPanel from "./RightPanel";
import Draggable from 'react-draggable';
import { PANEL_DEFAULT_WIDTH, PANEL_MAX_WIDTH } from "./Constants"

const DragBar = (props) => (
  <Draggable
    axis="x"
    onDrag={(ev, data) => props.onDrag(props.id, data.deltaX)}
  >
    <div className="dragbar" style={props.style} />
  </Draggable>
);

export default class ColumnLayout extends React.Component {
  constructor(props) {
    super(props);
    this.state = {
      left: PANEL_DEFAULT_WIDTH,
      right: PANEL_DEFAULT_WIDTH
    }
  }

  onDrag(id, deltaX) {
    const x = id === "left"
      ? this.state[id] + deltaX
      : this.state[id] - deltaX;
    this.setState({ [id]: x })
  }

  render() {
    return (
      <div id="layout-wrapper">
        <DragBar
          id="left"
          style={{left:PANEL_DEFAULT_WIDTH}}
          onDrag={this.onDrag.bind(this)} />
        <DragBar
          id="right"
          style={{right:PANEL_DEFAULT_WIDTH}}
          onDrag={this.onDrag.bind(this)} />
        <div id="layout">
          <LeftPanel width={this.state.left} />
          <CenterPanel />
          <RightPanel width={this.state.right} />
        </div>
      </div>
    );
  }
}
