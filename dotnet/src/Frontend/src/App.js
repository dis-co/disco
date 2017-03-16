import css from "../css/main.less"
import css2 from "react-grid-layout/css/styles.css"

import values from "./values"
import GlobalModel from "./GlobalModel"

import React, { Component } from 'react'
import PanelLeft from './PanelLeft'
import Tabs from './Tabs'
import Workspace from './Workspace'
import DropdownMenu from './widgets/DropdownMenu'

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
        <header id="app-header">
          <h1>Iris</h1>
          <DropdownMenu options={{
            "Project": () => console.log("Project"),
            "Shutdown": () => console.log("Shutdown"),
          }} />
        </header>
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
          <p>© 2017 - <a href="http://nsynk.de/">NSYNK Gesellschaft für Kunst und Technik mbH</a></p>
        </footer>
      </div>      
    );
  }
}
