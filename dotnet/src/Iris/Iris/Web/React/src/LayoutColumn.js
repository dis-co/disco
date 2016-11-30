import * as React from "react";
import PanelLeft from "./PanelLeft";
import PanelCenter from "./PanelCenter";
import PanelRight from "./PanelRight";
import Draggable from 'react-draggable';
import ModalLogin from "./ModalLogin";
import { PANEL_DEFAULT_WIDTH, PANEL_MAX_WIDTH, SKIP_LOGIN } from "./Constants"

const DragBar = (props) => (
  <Draggable
    axis="x"
    onDrag={(ev, data) => props.onDrag(props.id, data.deltaX)}
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

  onDrag(id, deltaX) {
    const x = id === "left"
      ? this.state[id] + deltaX
      : this.state[id] - deltaX;
    this.setState({ [id]: x })
  }

  render() {
    return (
      <div id="column-layout-wrapper">
        {SKIP_LOGIN ? null : <ModalLogin info={this.props.info} />}
        <DragBar
          id="left"
          style={{left:PANEL_DEFAULT_WIDTH}}
          onDrag={this.onDrag.bind(this)} />
        <DragBar
          id="right"
          style={{right:PANEL_DEFAULT_WIDTH}}
          onDrag={this.onDrag.bind(this)} />
        <div id="column-layout">
          <PanelLeft width={this.state.left} />
          <PanelCenter info={this.props.info} />
          <PanelRight width={this.state.right} />
        </div>
      </div>
    );
  }
}
