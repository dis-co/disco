import React from 'react';
import Tabs from 'muicss/lib/react/tabs';
import Tab from 'muicss/lib/react/tab';
import ReactGridLayout from 'react-grid-layout';
// import {Responsive, WidthProvider} from 'react-grid-layout';
// const ResponsiveReactGridLayout = WidthProvider(Responsive);
import { EMPTY } from 'iris';
import WidgetCluster from './widgets/Cluster';
import WidgetLog from './widgets/Log';
import WidgetDiscovery from './widgets/Discovery';

const rowHeight = 30;
function calculateCols(width) {
  return ~~(width/50);
}

function renderWidgets(info) {
  var widgets = [];

  if (info.state.Project != null && info.state.Project.Name !== EMPTY) {
      widgets.push(<div key={WidgetCluster.name} data-grid={WidgetCluster.layout}>
        <WidgetCluster info={info} />
      </div>);
  }

  widgets.push(
    <div key={WidgetLog.name} data-grid={WidgetLog.layout}>
      <WidgetLog context={info.context} />
    </div>,
    <div key={WidgetDiscovery.name} data-grid={WidgetDiscovery.layout}>
      <WidgetDiscovery />
    </div>
  );

  return widgets;
}

export default function PanelCenter(props) {
  // console.log("Panel Center Columns:", calculateCols(props.width))

  return (
    <div id="panel-center">
      <Tabs>
        <Tab label="CLUSTER VIEW" >
          <ReactGridLayout
            className="layout"
            cols={calculateCols(props.width)}
            width={props.width}
            rowHeight={rowHeight}
            verticalCompact={false}
            draggableHandle=".draggable-handle"
          >
            {renderWidgets(props.info)}
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
