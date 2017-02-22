import css from "../css/main.less"
import React, { Component } from 'react'
import ReactGridLayout from 'react-grid-layout'
import Spread from './widgets/Spread'
import Form from './Form'
import Log from './widgets/Log'

export default class App extends Component {
  constructor(props) {
    super(props);
    // this.state = { rows: [1,2,3,4,5], value: "W: 1920, H: 1080" };
    this.state = {};
  }

  componentDidMount() {
    $('#ui-layout-container')
      .layout({
        west__size: 150,
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
            cols={12}
            rowHeight={30}
            width={1200}
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
