import React, { Component } from 'react';
import Layout from './Layout';
import Counter from './Counter';
import Spread from './widgets/Spread';
import Form from './Form';

// If you use React Router, make this component
// render <Router> with your routes. Currently,
// only synchronous routes are hot reloaded, and
// you will see a warning from <Router> on every reload.
// You can ignore this warning. For details, see:
// https://github.com/reactjs/react-router/issues/2182
export default class App extends Component {
  constructor(props) {
    super(props);
    this.state = { rows: [1,2,3,4,5], value: "W: 1920, H: 1080" };
  }

  render() {
    return (
      <div>
        <h1>IRIS</h1>
        <Form globalState={this.state} setGlobalState={x => this.setState(x) } />
        <Spread rows={this.state.rows} value={this.state.value} />
      </div>
    );
  }
}
