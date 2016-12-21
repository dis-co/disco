import React from 'react';
import Tabs from 'muicss/lib/react/tabs';
import Tab from 'muicss/lib/react/tab';
import TreeView from './widgets/TreeView';

import treeData from "./data/tree.js";

export default function PanelLeft(props) { return (
  <Tabs id="panel-left" style={{width: props.width}}>
    <Tab label="PROJECT" >
      <TreeView data={treeData} />
    </Tab>
    <Tab label="VIEWS" >
      <TreeView data={treeData} />
    </Tab>
    <Tab label="LIBRARY" >
      <div>
        <p>Id excepteur cupidatat proident fugiat.</p>
      </div>
    </Tab>
  </Tabs>
)}
