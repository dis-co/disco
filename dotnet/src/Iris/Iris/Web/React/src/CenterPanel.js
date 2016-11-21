import React from 'react';
import {Tabs, Tab} from 'material-ui/Tabs';

export default function CenterPanel() { return (
  <Tabs>
    <Tab label="CUSTOM VIEW" >
      <div>
        <p>Laboris cillum ut cillum dolore velit excepteur qui ea non incididunt in officia sit magna.</p>
      </div>
    </Tab>
    <Tab label="GRAPH VIEW" >
      <div>
        <p>Id excepteur cupidatat proident fugiat.</p>
      </div>
    </Tab>
  </Tabs>
)}
