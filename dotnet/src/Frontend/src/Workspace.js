import values from "./values.js"
import { map } from "./Util.ts"
import React, { Component } from 'react'
import ReactGridLayout from 'react-grid-layout'
import Widget from "./widgets/Widget"

export default class Workspace extends Component {
  static get isFixed() {
    return true;
  }

  static get name() {
    return "Workspace";
  }

  constructor(props) {
    super(props);
    this.state = {};
  }

  componentDidMount() {
    this.disposable =
      this.props.global.subscribe("widgets", widgets => {
        this.setState({ widgets: new Map(widgets) });
      });
  }

  componentWillUnmount() {
    if (this.disposable) {
      this.disposable.Dispose();
    }
  }

  renderWidgets() {
    const widgets = this.state.widgets || this.props.global.state.widgets;
    return map(widgets, kv => {
      const id = kv[0], model = kv[1], View = model.view
      return (<div key={id} data-grid={model.layout}>
        <Widget id={id} global={this.props.global} model={model}>
          <View id={id} global={this.props.global} model={model} />
        </Widget>
      </div>)
    });
  }

  render() {
    return (
      <ReactGridLayout className="iris-workspace"
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
