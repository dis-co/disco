import values from "./values.js"
import { map } from "./Util.ts"
import React, { Component } from 'react'
import ReactGridLayout from 'react-grid-layout'
// import css from "../css/PanelCenter.less";
import Widget from "./widgets/Widget"

export default class PanelCenter extends Component {
  constructor(props) {
    super(props);
    this.state = {};
  }

  componentDidMount() {
    this.disposable =
      this.props.model.subscribe("widgets", widgets => {
        this.setState({ widgets });
      });
  }

  componentWillUnmount() {
    if (this.disposable) {
      this.disposable.dispose();
    }
  }

  renderWidgets() {
    const widgets = this.state.widgets || this.props.model.state.widgets;
    return map(widgets, kv => {
      const id = kv[0], Body = kv[1];
      return (<div key={id} data-grid={Body.layout}>
        <Widget id={id} model={this.props.model} body={Body} />
      </div>)
    });
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
