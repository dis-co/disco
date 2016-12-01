import React from 'react';
import Tabs from 'muicss/lib/react/tabs';
import Tab from 'muicss/lib/react/tab';
import {Responsive, WidthProvider} from 'react-grid-layout';
const ResponsiveReactGridLayout = WidthProvider(Responsive);
import WidgetCluster from './widgets/Cluster';
import widgetLayouts from './data/widgetLayouts';

const rowHeight = 30;
const layouts = { lg: widgetLayouts, md: widgetLayouts, sm: widgetLayouts };
const cols = { lg: 12, md: 12, sm: 12 };

export default function PanelCenter(props) { return (
  <div id="panel-center">
    <Tabs>
      <Tab label="CLUSTER VIEW" >
        {/* TODO: Width must be recalculated when side panels are resized */}
        <ResponsiveReactGridLayout
          className="layout"
          layouts={layouts}
          cols={cols}
          rowHeight={rowHeight}
          verticalCompact={false}
        >
          <div key={'cluster'}>
            <WidgetCluster info={props.info} />
          </div>
        </ResponsiveReactGridLayout>
      </Tab>
      <Tab label="GRAPH VIEW" >
        <div>
          <p>Laboris cillum ut cillum dolore velit excepteur qui ea non incididunt in officia sit magna.</p>
        </div>
      </Tab>
    </Tabs>
  </div>
)}
