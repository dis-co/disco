import React, { Component } from 'react';
import ReactGridLayout from 'react-grid-layout';
import Spread from './widgets/Spread';
import Form from './Form';
import Log from './widgets/Log';

const initSideWidth = 150;
const initItemLayout = {
  x: 0, y: 0,
  w: 2, h: 2,
  minW: 1, maxW: 10,
  minH: 1, maxH: 10
}

export default class App extends Component {
  constructor(props) {
    super(props);
    // this.state = { rows: [1,2,3,4,5], value: "W: 1920, H: 1080" };
    this.state = {};
  }

  componentDidMount() {
    $('#ui-layout-container')
      .layout({
        west__size: initSideWidth,
        center__onresize: (name, el, state) => {
          this.setState({centerWidth: state.innerWidth})
        }
    })
  }

  render() {
    let centerWidth = this.state.centerWidth;
    if (centerWidth == null) {
      centerWidth = $("#app").innerWidth() - (initSideWidth * 2);
    }

    console.log("Grid layout width", centerWidth)

    return (
      <div id="ui-layout-container" style={{height: "100%"}}>
        <div className="ui-layout-west" style={{height: "100%"}}>
          <h1>Hello World!</h1>
        </div>
        <div className="ui-layout-center" style={{
          height: "100%",
          background: "#595959"
        }}>
          <ReactGridLayout
            cols={12}
            rowHeight={30}
            width={centerWidth}
            verticalCompact={false}
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
