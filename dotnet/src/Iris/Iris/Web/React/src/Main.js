import * as React from "react";
import * as ReactDom from "react-dom";
import injectTapEventPlugin from 'react-tap-event-plugin';
import MuiThemeProvider from 'material-ui/styles/MuiThemeProvider';
import Cluster from "./Cluster.js";

function map(xs, f) {
  var ar = [];
  for (const x of xs)
    ar.push(f(x))
  return ar;
}

injectTapEventPlugin();

class App extends React.Component {
  constructor(props, ctx) {
    super(props, ctx);
    props.subscribe(state => this.setState(state))
  }

  render() {
    if (this.state == null) {
      return <div>Loading...</div>;
    }
    else {
      return <MuiThemeProvider>
        <Cluster nodes={map(this.state.Nodes, x => x[1])} />
      </MuiThemeProvider>
    }
  }
}

export default {
  mount(subscribe) {
    ReactDom.render(<App subscribe={subscribe} />, document.getElementById("app"))
  }
}
