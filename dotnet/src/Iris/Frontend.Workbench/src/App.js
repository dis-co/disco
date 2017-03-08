import css from "../css/main.less"
import css2 from "react-grid-layout/css/styles.css"

import values from "./values"
import GlobalModel from "./GlobalModel"

import React, { Component } from 'react'
import PanelLeft from './PanelLeft'
import Tabs from './Tabs'
import Workspace from './Workspace'

export default class App extends Component {
  constructor(props) {
    super(props);
    this.global = new GlobalModel();
    this.global.addTab({
      name: "Workspace",
      view: Workspace,
      isFixed: true
    });
  }

  componentDidMount() {
    this.layout =
      $('#ui-layout-container')
        .layout({
          west__size: values.jqueryLayoutWestSize,
          center__onresize: (name, el, state) => {
            this.setState({centerWidth: state.innerWidth})
          }
      });
  }

  render() {
    return (
      <div id="ui-layout-container">
        <div className="ui-layout-west">
          <PanelLeft global={this.global} />
        </div>
        <div className="ui-layout-center">
          <Tabs global={this.global} />
        </div>
      </div>
    );
  }
}