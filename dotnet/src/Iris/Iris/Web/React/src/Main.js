import * as React from "react";
import * as ReactDom from "react-dom";
import injectTapEventPlugin from 'react-tap-event-plugin';
import MuiThemeProvider from 'material-ui/styles/MuiThemeProvider';
import Layout from "./Layout";

injectTapEventPlugin();

class App extends React.Component {
  constructor(props) {
    super(props);
    props.subscribe(state => {
      console.log(state);
      this.setState(state);
    })
  }

  render() {
    return <MuiThemeProvider>
      <Layout state={this.state} />
    </MuiThemeProvider>
  }
}

export default {
  mount(subscribe) {
    ReactDom.render(<App subscribe={subscribe} />, document.getElementById("app"))
  }
}
