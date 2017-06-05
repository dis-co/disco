import "bulma/css/bulma.css"
import "../css/main.less"
import "react-grid-layout/css/styles.css"

import values from "./values"
import { GlobalModel } from "../fable/Frontend/GlobalModel.fs"

import React, { Component } from 'react'
import PanelLeft from './PanelLeft'
import Tabs from './Tabs'
import Workspace from './Workspace'
import NavBar, { showModal as showModalNavBar } from './widgets/NavBar'

export function showModal(content, props) {
  return showModalNavBar(content, props);
}

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
      <div id="app">
        <NavBar global={this.global} />
        <div id="app-content">
          <div id="ui-layout-container">
            <div className="ui-layout-west">
              <PanelLeft global={this.global} />
            </div>
            <div className="ui-layout-center">
              <Tabs global={this.global} />
            </div>
          </div>
        </div>
        <footer id="app-footer">
          <p>© 2017 - <a href="http://nsynk.de/">NSYNK Gesellschaft für Kunst und Technik GmbH</a></p>
        </footer>
      </div>
    );
  }
}
