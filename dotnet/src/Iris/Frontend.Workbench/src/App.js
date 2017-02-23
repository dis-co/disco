import css from "../css/main.less"
import css2 from "react-grid-layout/css/styles.css"

import values from "./values.js"
import React, { Component } from 'react'
import ReactGridLayout from 'react-grid-layout'
import Spread from './widgets/Spread'
import Form from './Form'
import Log from './widgets/Log'

export default class App extends Component {
  constructor(props) {
    super(props);
    this.state = {};
  }

  componentDidMount() {
    $('#ui-layout-container')
      .layout({
        west__size: values.jqueryLayoutWestSize,
        center__onresize: (name, el, state) => {
          this.setState({centerWidth: state.innerWidth})
        }
    })
  }

  render() {
    return (
      <div id="ui-layout-container">
        <div className="ui-layout-west">
          <h1>Hello World!</h1>
        </div>
        <div className="ui-layout-center">
          <ReactGridLayout
            cols={values.gridLayoutColumns}
            rowHeight={values.gridLayoutRowHeight}
            width={values.gridLayoutWidth}
            verticalCompact={false}
            draggableHandle=".iris-draggable-handle"
          >
            <div key={0} data-grid={Log.layout}>
              <Log />
            </div>
          </ReactGridLayout>
        </div>
      </div>
    );
  }
}
