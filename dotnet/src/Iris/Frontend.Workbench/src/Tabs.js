import React, { Component } from 'react'
import { map, head, last } from "./Util.ts"

export default class Tabs extends Component {
  constructor(props) {
    super(props);
    this.state = {};
  }

  componentDidMount() {
    this.disposable =
      this.props.global.subscribe("tabs", tabs => {
        let selected = null;
        // Tab added, select it
        if (this.state.tabs == null || this.state.tabs.size < tabs.size) {
          selected = last(tabs);
        }
        this.setState({ tabs: new Map(tabs), selected });
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

      let spans = [<span key={0}>{tab.name}</span>];
      if (!tab.isFixed) {
        spans.push(
          <span key={1} className="ui-icon ui-icon-copy" onClick={ev => {
            ev.stopPropagation();
            this.props.global.addWidget(id, tab);
            this.props.global.removeTab(id);
          }}></span>,
          <span key={2} className="ui-icon ui-icon-close" onClick={ev => {
            ev.stopPropagation();
            this.props.global.removeTab(id);
          }}></span>
        )
      }

      return (
        <div key={id} className={className} onClick={() => {
          console.log("tab " + id + " clicked");
          this.setState({ selected: kv })
        }}>
          {spans}
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
