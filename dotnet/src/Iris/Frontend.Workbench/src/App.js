import React, { Component } from 'react';
import Spread from './widgets/Spread';
import Form from './Form';

const sideInitWidth = 150;

export default class App extends Component {
  constructor(props) {
    super(props);
    this.state = { rows: [1,2,3,4,5], value: "W: 1920, H: 1080" };
  }

  componentDidMount() {
    $('#ui-layout-container')
      .layout({
        west__size: sideInitWidth,
        // center__onresize: (name, el, state) => {
        //   this.setState({centerWidth: state.innerWidth})
        // }
    })
  }

  render() {
    return (
      <div id="ui-layout-container" style={{height: "100%"}}>
        <div className="ui-layout-west" style={{height: "100%"}}>
          <h1>Hello World!</h1>
        </div>
        <div className="ui-layout-center" style={{height: "100%"}}>
          <h1>Hello World!</h1>
        </div>
      </div>
    );
  }
}
