import * as React from "react";
import * as ReactDom from "react-dom";

import _ReactGridLayout, { WidthProvider } from "react-grid-layout";
const ReactGridLayout = WidthProvider(_ReactGridLayout);

require("react-grid-layout/css/styles.css");
require("react-resizable/css/styles.css");

let defaultLayout = [
  {i: 'side1', x: 0, y: 0, w: 3, h: 12},
  {i: 'main', x: 3, y: 0, w: 6, h: 12},
  {i: 'side2', x: 9, y: 0, w: 3, h: 12}
];

class App extends React.Component {
  onLayoutChange(layout) {
    layout.sort((a,b) => a.x < b.x ? -1 : 1); // Sort panels
    layout[0].x = 0;
    layout[1].x = layout[0].w;
    layout[2].x = layout[1].x + layout[1].w;
    for (let i = 0; i < layout.length; i++)
      layout[i].y = 0;
    this.setState({layout})
  }
  render() {
    return (
      <ReactGridLayout
          className="layout" onLayoutChange={this.onLayoutChange.bind(this)}
          layout={defaultLayout} cols={12} rowHeight={100} >
        <div key={'side1'} style={{background: "lightgrey"}}>SIDE 1</div>
        <div key={'main'} style={{background: "lightblue"}}>MAIN</div>
        <div key={'side2'} style={{background: "lightgrey"}}>SIDE 2</div>
      </ReactGridLayout>
    )
  }
};

export default {
  mount(subscribe) {
    ReactDom.render(<App subscribe={subscribe} />, document.getElementById("app"))
  }
}
