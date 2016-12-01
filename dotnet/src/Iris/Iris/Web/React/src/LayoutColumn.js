import * as React from "react";
import PanelLeft from "./PanelLeft";
import PanelCenter from "./PanelCenter";
import PanelRight from "./PanelRight";
import Draggable from 'react-draggable';
import { PANEL_DEFAULT_WIDTH, PANEL_MAX_WIDTH, SKIP_LOGIN } from "./Constants"

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
    this.state = {
      left: PANEL_DEFAULT_WIDTH,
      right: PANEL_DEFAULT_WIDTH
    }
  }

  onDrag(side, deltaX) {
    const x = side === "left"
      ? this.state[side] + deltaX
      : this.state[side] - deltaX;
    this.setState({ [side]: x })
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
          <PanelCenter info={this.props.info} />
          <PanelRight width={this.state.right} />
        </div>
      </div>
    );
  }
}
