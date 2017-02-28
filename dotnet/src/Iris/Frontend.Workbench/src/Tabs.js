import React, { Component } from 'react'
import { map, head } from "./Util.ts"

export default class Tabs extends Component {
  constructor(props) {
    super(props);
    this.state = {};
  }

  componentDidMount() {
    this.disposable =
      this.props.global.subscribe("tabs", tabs => {
        this.setState({ tabs });
      });
  }

  componentWillUnmount() {
    if (this.disposable) {
      this.disposable.dispose();
    }
  }

  renderTabs(tabs, selected) {
    const selectedId = selected != null ? selected[0] : -1;
    return map(tabs, kv => {
      const id = kv[0], tab = kv[1];
      const className = id === selectedId
        ? "iris-tab-name iris-selected"
        : "iris-tab-name";
      return (
        <div key={id} className={className} onClick={() => {
          console.log("tab " + id + " clicked");
          this.setState({ selected: kv })
        }}>
          <span>{tab.name}</span>
          {!tab.isFixed ?
            <span className="ui-icon ui-icon-close" onClick={ev => {
              ev.stopPropagation();
              this.setState({ selected: null });
              this.props.global.removeTab(id);
            }}></span> : null }
        </div>
      )
    });
  }

  renderBody(selected) {
    if (selected != null) {
      const Body = selected[1].view;
      return <Body global={this.props.global} model={selected[1]} />
    }
    return null;
  }

  render() {
    const tabs = this.state.tabs || this.props.global.state.tabs;
    const selected = this.state.selected || head(tabs);
    return (
      <div className="iris-tab-container">
        <div className="iris-tab-name-container">
          {this.renderTabs(tabs, selected)}
        </div>
        <div className="iris-tab-body">
          {this.renderBody(selected)}
        </div>
      </div>
    )
  }
}
