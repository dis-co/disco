import * as React from "react";
import css from "../css/PanelLeft.less";

const Square = props => (
  <div className="iris-panel-left-child">
    <div>P</div>
    <div>
      <p><strong>Project Overview (Big)</strong></p>
      <p>Cluster Settings</p>
    </div>
  </div>
);

export default class PanelLeft extends React.Component {
  constructor(props) {
    super(props);
  }

  render() {
    return (
      <div className="iris-panel-left">
        <Square />
        <Square />
        <Square />
      </div>
    )
  }
}
