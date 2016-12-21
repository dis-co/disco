import React from 'react';
import cx from 'classnames';

import Tree from 'react-ui-tree';
import "react-ui-tree/dist/react-ui-tree.css";
import "../styles/tree.less";

export default class TreeView extends React.Component {
  constructor(props) {
    super(props);
    this.state = { active: null, tree: props.data };
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

