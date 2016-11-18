import * as React from "react";
import LoginDialog from "./LoginDialog";
import LeftPanel from "./LeftPanel";
import CenterPanel from "./CenterPanel";
import RightPanel from "./RightPanel";
import _ReactGridLayout, { WidthProvider } from "react-grid-layout";
const ReactGridLayout = WidthProvider(_ReactGridLayout);

require("react-grid-layout/css/styles.css");
require("react-resizable/css/styles.css");

let defaultLayout = [
  {i: 'left', x: 0, y: 0, w: 3, h: 12, isDraggable: false},
  {i: 'center', x: 3, y: 0, w: 6, h: 12},
  {i: 'right', x: 9, y: 0, w: 3, h: 12}
];

export default class Layout extends React.Component {
  onLayoutChange(layout) {
    layout.sort((a,b) => a.x < b.x ? -1 : 1); // Sort panels
    layout[0].x = 0;
    layout[1].x = layout[0].w;
    layout[2].x = layout[1].x + layout[1].w;
    layout[2].w = 12 - layout[2].x;
    for (let i = 0; i < layout.length; i++)
      layout[i].y = 0;
    this.setState({layout})
  }
  render() {
    return (
      <div>
        <LoginDialog />
        <ReactGridLayout
            className="layout" onLayoutChange={this.onLayoutChange.bind(this)}
            layout={defaultLayout} cols={12} rowHeight={65} >
          <div key={'left'} style={{background: "lightgrey"}}>
            <LeftPanel />
          </div>
          <div key={'center'} style={{background: "lightblue"}}>
            <CenterPanel />
          </div>
          <div key={'right'} style={{background: "lightgrey"}}>
            <RightPanel />
          </div>
        </ReactGridLayout>
      </div>
    )
  }
}