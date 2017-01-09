import React from 'react';
import Tabs from 'muicss/lib/react/tabs';
import Tab from 'muicss/lib/react/tab';
import TreeView from './widgets/TreeView';
import * as treeData from "./data/tree.js";
import { map } from './Util.ts';
import { EMPTY, project2tree } from 'iris';

// function projectDataFilter(k, v) {
//   switch (v.constructor.name) {
//     case "Id":
//       return { module: `${k}: ${v.Fields[0]}`};
//     case "LogLevel":
//       return { module: `${k}: ${v.Case}`};
//     case "FMap":
//       return {
//           module: k,
//           children: map(v, (kv, i) => treeData.object2tree(kv[1], "Member" + i, projectDataFilter))
//       }
//   }
// }

function renderProject(project) {
  if (project != null && project.Name !== EMPTY) {
    return <TreeView data={project2tree(project)} />
  }
}

export default function PanelLeft(props) {
  return (
    <Tabs id="panel-left" style={{width: props.width}}>
      <Tab label="PROJECT" >
        {renderProject(props.info.state.Project)}
      </Tab>
      <Tab label="VIEWS" >
        <TreeView data={treeData.sample} editable={true} />
      </Tab>
    </Tabs>
  )
}
