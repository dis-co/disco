import React from 'react';
import cx from 'classnames';
import Tabs from 'muicss/lib/react/tabs';
import Tab from 'muicss/lib/react/tab';

import Tree from 'react-ui-tree';
import "react-ui-tree/dist/react-ui-tree.css";
import "./styles/tree.less";

import treeData from "./data/tree.js";

class TreeView extends React.Component {
  constructor(props) {
    super(props);
    this.state = { active: null, tree: treeData };
  }

  renderNode(node) {
    return (
      <span className={cx('node', {
        'is-active': node === this.state.active
        })} onClick={this.onClickNode.bind(this, node)}>
        {node.module}
      </span>
    );
  }

  handleChange(newTree) {
    this.setState({
      tree: newTree
    });
  }

  onClickNode(node) {
    this.setState({
      active: node
    });
  }

  render() {
    return (
      <Tree
        paddingLeft={20}
        tree={this.state.tree}
        onChange={this.handleChange.bind(this)}
        // isNodeCollapsed={this.isNodeCollapsed.bind(this)}
        renderNode={this.renderNode.bind(this)}
      />
    )
  }
}

export default function PanelLeft(props) { return (
  <Tabs id="panel-left" style={{width: props.width}}>
    <Tab label="VIEWS" >
      <TreeView />
    </Tab>
    <Tab label="LIBRARY" >
      <div>
        <p>Id excepteur cupidatat proident fugiat.</p>
      </div>
    </Tab>
  </Tabs>
)}
