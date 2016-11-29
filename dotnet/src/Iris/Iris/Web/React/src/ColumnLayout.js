import * as React from "react";
import LeftPanel from "./LeftPanel";
import CenterPanel from "./CenterPanel";
import RightPanel from "./RightPanel";
import Draggable from 'react-draggable';
import LoginDialog from "./LoginDialog";
import { PANEL_DEFAULT_WIDTH, PANEL_MAX_WIDTH, SKIP_LOGIN } from "./Constants"

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
      <div id="column-layout-wrapper">
        {SKIP_LOGIN ? null :
          <LoginDialog
            login={this.props.login}
            session={this.props.session} />}
        <DragBar
          id="left"
          style={{left:PANEL_DEFAULT_WIDTH}}
          onDrag={this.onDrag.bind(this)} />
        <DragBar
          id="right"
          style={{right:PANEL_DEFAULT_WIDTH}}
          onDrag={this.onDrag.bind(this)} />
        <div id="column-layout">
          <LeftPanel width={this.state.left} />
          <CenterPanel state={this.props.state} />
          <RightPanel width={this.state.right} />
        </div>
      </div>
    );
  }
}
