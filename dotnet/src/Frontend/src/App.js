import css from "../css/main.less"
import css2 from "react-grid-layout/css/styles.css"

import values from "./values"
import GlobalModel from "./GlobalModel"

import React, { Component } from 'react'
import PanelLeft from './PanelLeft'
import Tabs from './Tabs'
import Workspace from './Workspace'
import DropdownMenu from './DropdownMenu'
import ModalDialog from './modals/ModalDialog'
import CreateProject from './modals/CreateProject'
import LoadProject from './modals/LoadProject'
import SimpleModal from './modals/Simple'

let modal = null;

export function showModal(content, onSubmit) {
  modal.setState({ content, onSubmit });
}

export function askModal(title, text, buttons) {
  buttons = buttons || [["Yes", true], ["No", false]];
  return new Promise((resolve, reject) => {
    modal.setState({
      content: SimpleModal,
      props: { title, text, buttons },
      onSubmit: resolve
    })
  });
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
    var global = this.global;
    return (
      <div id="app">
        <ModalDialog ref={el => modal = (el || modal)} />        
        <header id="app-header">
          <h1>Iris</h1>
          <DropdownMenu options={[
            {
              label: () => "Create project",
              action: () => showModal(CreateProject),
            },
            {
              label: () => "Load project",
              action: () => showModal(LoadProject),
            },
            {
              label: () => "Unload project",
              action: () => Iris.unloadProject(),
            },
            {
              label: () => "Shutdown",
              action: () => Iris.shutdown(),
            },
            {
              label: () => "Use right click: " + global.state.useRightClick,
              action: () => global.useRightClick(!global.state.useRightClick),
            }            
          ]} />
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
