import React from 'react';
import Tabs from 'muicss/lib/react/tabs';
import Tab from 'muicss/lib/react/tab';
import ReactGridLayout from 'react-grid-layout';
// import {Responsive, WidthProvider} from 'react-grid-layout';
// const ResponsiveReactGridLayout = WidthProvider(Responsive);
import { WIDGETS } from './Constants'
import widgetLayouts from './data/widgetLayouts';
import WidgetCluster from './widgets/Cluster';
import WidgetLog from './widgets/Log';

const rowHeight = 30;
function calculateCols(width) {
  return ~~(width/50);
}

export default function PanelCenter(props) {
  // console.log("Panel Center Columns:", calculateCols(props.width))
  return (
    <div id="panel-center">
      <Tabs>
        <Tab label="CLUSTER VIEW" >
          <ReactGridLayout
            className="layout"
            layout={widgetLayouts}
            cols={calculateCols(props.width)}
            width={props.width}
            rowHeight={rowHeight}
            verticalCompact={false}
          >
            <div key={WIDGETS.CLUSTER}>
              <WidgetCluster info={props.info} />
            </div>
            <div key={WIDGETS.LOG}>
              <WidgetLog context={props.info.context} />
            </div>
          </ReactGridLayout>
        </Tab>
        <Tab label="GRAPH VIEW" >
          <div>
            <p>Laboris cillum ut cillum dolore velit excepteur qui ea non incididunt in officia sit magna.</p>
          </div>
        </Tab>
      </Tabs>
    </div>
  )
}
