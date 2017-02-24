import css from "../css/main.less"
import css2 from "react-grid-layout/css/styles.css"

import values from "./values"
import Model from "./Model"

import React, { Component } from 'react'
import PanelLeft from './PanelLeft'
import PanelCenter from './PanelCenter'

export default class App extends Component {
  constructor(props) {
    super(props);
    this.model = new Model();
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
          <PanelLeft model={this.model} />
        </div>
        <div className="ui-layout-center">
          <PanelCenter model={this.model} />
        </div>
      </div>
    );
  }
}
