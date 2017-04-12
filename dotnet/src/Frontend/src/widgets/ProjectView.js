import React, { Component } from 'react'
import Tree from "../../lib/react-ui-tree/react-ui-tree"
import "../../lib/react-ui-tree/react-ui-tree.less"

class ProjectView extends Component {
  constructor(props) {
    super(props);
  }

  componentDidMount() {
    this.disposable =
      this.props.global.subscribe("project", () => {
        this.forceUpdate();
      });
  }

  componentWillUnmount() {
    if (this.disposable) {
      this.disposable.dispose();
    }
  }

  render() {
    const project = this.props.global.state.project;
    if (project == null) {
      return <p>No project loaded</p>;
    }
    else {
      return (
        <Tree
          paddingLeft={20}             
          tree={Iris.project2tree(project)}      
          freeze={true}  
          renderNode={node => <span>{node.module}</span>}            
        />
      )
    }
  }
}

export default class ProjectModel {
  constructor() {
    this.view = ProjectView;
    this.name = "Project View";
    this.layout = {
      x: 0, y: 0,
      w: 3, h: 6,
      minW: 1, maxW: 10,
      minH: 1, maxH: 10
    }
  }
}