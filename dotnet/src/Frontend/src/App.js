import "bulma/css/bulma.css"
import "../css/main.less"
import "react-grid-layout/css/styles.css"

import values from "./values"
import { GlobalModel } from "../fable/Frontend/GlobalModel.fs"
import * as util from "./Util"

import React, { Component } from 'react'
import PanelLeft from './PanelLeft'
import Tabs from './Tabs'
import Workspace from './Workspace'
import DropdownMenu from './DropdownMenu'
import ModalDialog from './modals/ModalDialog'
import CreateProject from './modals/CreateProject'
import SimpleModal from './modals/Simple'

let modal = null;

export function showModal(content, props) {
  props = props != null ? props : {};
  return new Promise(resolve =>
    modal.setState({ content, props, onSubmit: resolve })
  );
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
    this.global.subscribe("serviceInfo", serviceInfo => this.setState({serviceInfo}));
    this.state = {
      serviceInfo: this.global.state.serviceInfo
    };
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
              action: () => util.loadProject(),
            },
            {
              label: () => "Unload project",
              action: () => IrisLib.unloadProject(),
            },
            {
              label: () => "Shutdown",
              action: () => IrisLib.shutdown(),
            },
            {
              label: () => "Use right click: " + global.state.useRightClick,
              action: () => global.useRightClick(!global.state.useRightClick),
            }
          ]} />
          <div className="separator" />
          <span>Iris v{this.state.serviceInfo.version} - build {this.state.serviceInfo.buildNumber}</span>
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
          <p>© 2017 - <a href="http://nsynk.de/">NSYNK Gesellschaft für Kunst und Technik GmbH</a></p>
        </footer>
      </div>
    );
  }
}
