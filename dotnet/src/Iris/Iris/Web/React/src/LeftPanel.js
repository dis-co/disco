import React from 'react';
import cx from 'classnames';
import {Tabs, Tab} from 'material-ui/Tabs';

import Tree from 'react-ui-tree';
require("react-ui-tree/dist/react-ui-tree.css");
require("./styles/tree.less");

let treeChildren = [
{
  module: 'View 1',
  children: [
    {
      module: 'Widget 1',
      collapsed: true,
      children: [
        {module: 'IN'},
        {module: 'OUT', children: [
          {module:'Value'},
          {module:'Value'}
        ]}
      ]
    },
    {
      module: 'Widget 2',
      collapsed: true,
      children: [
        {module: 'IN'},
        {module: 'OUT', children: [
          {module:'Value'},
          {module:'Value'}
        ]}
      ]
    },
    {
      module: 'Widget 3',
      children: [
        {module: 'IN'},
        {module: 'OUT', children: [
          {module:'Value'},
          {module:'Value'}
        ]}
      ]
    }
  ]
},
{
  module: 'View 2',
  children: [
    {
      module: 'Widget 1',
      collapsed: true,
      children: [
        {module: 'IN'},
        {module: 'OUT', children: [
          {module:'Value'},
          {module:'Value'}
        ]}
      ]
    },
    {
      module: 'Widget 2',
      collapsed: true,
      children: [
        {module: 'IN'},
        {module: 'OUT', children: [
          {module:'Value'},
          {module:'Value'}
        ]}
      ]
    },
    {
      module: 'Widget 3',
      children: [
        {module: 'IN'},
        {module: 'OUT', children: [
          {module:'Value'},
          {module:'Value'}
        ]}
      ]
    }
  ]
},
{
  module: 'View 3',
  children: [
    {
      module: 'Widget 1',
      collapsed: true,
      children: [
        {module: 'IN'},
        {module: 'OUT', children: [
          {module:'Value'},
          {module:'Value'}
        ]}
      ]
    },
    {
      module: 'Widget 2',
      collapsed: true,
      children: [
        {module: 'IN'},
        {module: 'OUT', children: [
          {module:'Value'},
          {module:'Value'}
        ]}
      ]
    },
    {
      module: 'Widget 3',
      children: [
        {module: 'IN'},
        {module: 'OUT', children: [
          {module:'Value'},
          {module:'Value'}
        ]}
      ]
    }
  ]
},
];

class TreeView extends React.Component {
  constructor(props) {
    super(props);
    this.state = { active: null, tree: { children: treeChildren } };
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

  handleChange(tree) {
    this.setState({
      tree: tree
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

export default function () { return (
  <Tabs>
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
