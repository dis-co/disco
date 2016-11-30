import React from 'react';
import Tabs from 'muicss/lib/react/tabs';
import Tab from 'muicss/lib/react/tab';
import {Responsive, WidthProvider} from 'react-grid-layout';
const ResponsiveReactGridLayout = WidthProvider(Responsive);
import WidgetCluster from './WidgetCluster';

const layout = [{i: 'a', x: 0, y: 0, w: 4, h: 2 }];
const layouts = { lg: layout, md: layout, sm: layout };
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
          verticalCompact={false}
        >
          <div key={'a'}>
            <WidgetCluster nodes={props.info.state.Nodes} />
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
