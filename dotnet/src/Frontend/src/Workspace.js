import values from "./values.js"
import { map } from "./Util.ts"
import React, { Component } from 'react'
import ReactGridLayout from 'react-grid-layout'
import Widget from "./widgets/Widget"

function getFromLS() {
  debugger;
  let layout = [];
  if (global.localStorage) {
    try {
      layout = JSON.parse(global.localStorage.getItem('iris-workspace')) || [];
    } catch(e) { /*Ignore*/ }
  }
  return layout;
}

function saveToLS(layout) {
  if (global.localStorage) {
    global.localStorage.setItem('iris-workspace', JSON.stringify(layout));
  }
}

export default class Workspace extends Component {
  static get isFixed() {
    return true;
  }

  static get name() {
    return "Workspace";
  }

  constructor(props) {
    super(props);

    var widgets = props.global.state.widgets;
    this.layout = this.updateLayout(getFromLS(), widgets);
    this.state = { widgets };
  }

  updateLayout(layout, widgets) {
    layout = layout.filter(x => widgets.has(x.i));
    let layoutMap = new Map(layout.map(x => [x.i, x]));
    for (let [id, widget] of widgets) {
      if (!layoutMap.has(id)) {
        let l = widget.layout;
        l.i = id;
        layout.splice(layout.length - 1, 0, l);
      }
      else {
        widget.layout = layoutMap.get(id);
      }
    }
    return layout;
  }

  componentDidMount() {
    this.disposable =
      this.props.global.subscribe("widgets", widgets => {
        this.layout = this.updateLayout(this.layout.slice(), widgets)
        this.setState({ widgets: widgets });
      });
  }

  componentWillUnmount() {
    if (this.disposable) {
      this.disposable.dispose();
    }
  }

  renderWidgets() {
    return map(this.state.widgets, kv => {
      const id = kv[0], model = kv[1], View = model.view
      return (<div key={id}>
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
        layout={this.layout}
        onLayoutChange={layout => {
          const newLayout = this.updateLayout(layout.slice(), this.state.widgets);
          saveToLS(newLayout);
          this.layout = newLayout;
        }}
      >
        {this.renderWidgets()}
      </ReactGridLayout>
    )
  }
}
