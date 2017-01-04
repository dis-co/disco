import React from 'react';
import Tabs from 'muicss/lib/react/tabs';
import Tab from 'muicss/lib/react/tab';
import TreeView from './widgets/TreeView';
import * as treeData from "./data/tree.js";
import { map } from './Util.ts';

function projectDataFilter(k, v) {
  switch (v.constructor.name) {
    case "Id":
      return { module: `${k}: ${v.Fields[0]}`};
    case "LogLevel":
      return { module: `${k}: ${v.Case}`};
    case "FMap":
      return {
          module: k,
          children: map(v, (kv, i) => treeData.object2tree(kv[1], "Member" + i, projectDataFilter))
      }
  }
}


export default function PanelLeft(props) {
  let projectData = props.info.state.Project || {};
  return (
    <Tabs id="panel-left" style={{width: props.width}}>
      <Tab label="PROJECT" >
        <TreeView data={treeData.object2tree(projectData, "Project", projectDataFilter)} />
      </Tab>
      <Tab label="VIEWS" >
        <TreeView data={treeData.sample} editable={true} />
      </Tab>
      <Tab label="LIBRARY" >
        <div>
          <p>Id excepteur cupidatat proident fugiat.</p>
        </div>
      </Tab>
    </Tabs>
  )
}
