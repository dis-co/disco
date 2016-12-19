import * as React from "react";
import PanelLeft from "./PanelLeft";
import PanelCenter from "./PanelCenter";
import PanelRight from "./PanelRight";
import Draggable from 'react-draggable';
import { PANEL_DEFAULT_WIDTH, PANEL_MAX_WIDTH } from "./Constants"

function calculateWidths(prev) {
  const sideMax = window.innerWidth / 4;
  const left = Math.min(prev ? prev.left : PANEL_DEFAULT_WIDTH, sideMax);
  const right = Math.min(prev ? prev.right : PANEL_DEFAULT_WIDTH, sideMax);
  return { left, right, sideMax, center: window.innerWidth - (left + right) };
}

const DragBar = (props) => (
  <Draggable
    axis="x"
    onDrag={(ev, data) => props.onDrag(props.side, data.deltaX)}
  >
    <div className="dragbar" style={props.style} />
  </Draggable>
);

export default class LayoutColumn extends React.Component {
  constructor(props) {
    super(props);
    this.state = calculateWidths();
  }

  onDrag(side, deltaX) {
    let x = side === "left"
      ? this.state[side] + deltaX
      : this.state[side] - deltaX;
    if (x >= this.state.sideMax) {
      return false;
    }
    else {
      const prev = Object.assign({}, this.state, { [side]: x });
      this.setState(calculateWidths(prev))
    }
  }

  render() {
    return (
      <div className="column-layout-wrapper">
        <DragBar
          side="left"
          style={{left:PANEL_DEFAULT_WIDTH}}
          onDrag={this.onDrag.bind(this)} />
        <DragBar
          side="right"
          style={{right:PANEL_DEFAULT_WIDTH}}
          onDrag={this.onDrag.bind(this)} />
        <div className="column-layout">
          <PanelLeft width={this.state.left} />
          <PanelCenter width={this.state.center} info={this.props.info} />
          <PanelRight width={this.state.right} />
        </div>
      </div>
    );
  }
}
