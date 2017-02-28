import React, { Component } from 'react'
import { map, head } from "./Util.ts"

export default class Tabs extends Component {
  constructor(props) {
    super(props);
    this.state = {};
  }

  componentDidMount() {
    this.disposable =
      this.props.model.subscribe("tabs", tabs => {
        this.setState({ tabs });
      });
  }

  componentWillUnmount() {
    if (this.disposable) {
      this.disposable.dispose();
    }
  }

  renderTabs(tabs) {
    return map(tabs, kv => {
      const id = kv[0], Tab = kv[1];
      return (
        <div key={id} className="iris-tab-name" onClick={() => {
          console.log("tab " + id + " clicked");
          this.setState({ selectedTab: Tab })
        }}>
          <span>{Tab.name}</span>
          {!Tab.isFixed ?
            <span className="ui-icon ui-icon-close" onClick={ev => {
              ev.stopPropagation();
              this.setState({ selectedTab: null });
              this.props.model.removeTab(id);
            }}></span> : null }
        </div>
      )
    });
  }

  render() {
    const tabs = this.state.tabs || this.props.model.state.tabs;
    const Tab = this.state.selectedTab || head(tabs, kv => kv[1]);
    return (
      <div className="iris-tab-container">
        <div className="iris-tab-name-container">
          {this.renderTabs(tabs)}
        </div>
        <div className="iris-tab-body">
          {Tab != null ? <Tab model={this.props.model} /> : null}
        </div>
      </div>
    )
  }
}
