import React from 'react';
import cx from 'classnames';

import Tree from '../lib/react-ui-tree/react-ui-tree';
import "../lib/react-ui-tree/react-ui-tree.less";
import "../styles/tree.less";

export default class TreeView extends React.Component {
  constructor(props) {
    super(props);
    this.state = { active: null };
  }

  renderNode(node) {
    return (
      <span
        className={cx('node', {
          'is-active': node === this.state.active
          })}
        onClick={this.onClickNode.bind(this, node)}
      >
        {node.module}
      </span>
    );
  }

  // handleChange(newTree) {
  //   this.setState({
  //     tree: newTree
  //   });
  // }

  onClickNode(node) {
    this.setState({
      active: node
    });
  }

  render() {
    return (
      <Tree
        paddingLeft={20}
        tree={this.props.data}
        editable={this.props.editable}
        // onChange={this.handleChange.bind(this)}
        // isNodeCollapsed={this.isNodeCollapsed.bind(this)}
        renderNode={this.renderNode.bind(this)}
      />
    )
  }
}

