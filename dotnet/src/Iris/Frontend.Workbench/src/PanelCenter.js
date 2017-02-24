import values from "./values.js"
import React, { Component } from 'react'
import ReactGridLayout from 'react-grid-layout'
// import css from "../css/PanelCenter.less";

export default class PanelCenter extends Component {
  constructor(props) {
    super(props);
  }

  renderWidgets() {
    const model = this.props.model;
    return model.state.widgets.map((Widget, i) =>
      <div key={i} data-grid={Widget.layout}>
          <Widget model={model} />
        </div>
    )
  }

  render() {
    return (
      <ReactGridLayout
        cols={values.gridLayoutColumns}
        rowHeight={values.gridLayoutRowHeight}
        width={values.gridLayoutWidth}
        verticalCompact={false}
        draggableHandle=".iris-draggable-handle"
      >
        {this.renderWidgets()}
      </ReactGridLayout>
    )
  }
}
