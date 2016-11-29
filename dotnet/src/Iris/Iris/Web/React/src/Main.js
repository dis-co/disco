import * as React from "react";
import * as ReactDom from "react-dom";
import Layout from "./ColumnLayout";
import { getCurrentSession, login } from "lib";

class App extends React.Component {
  constructor(props) {
    super(props);
    props.subscribe(state => {
      console.log("Received state:",state);
      this.setState(state);
    })
  }

  render() {
    return (
      <Layout
        login={(username, password) => login(this.state, username, password)}
        session={this.state ? getCurrentSession(this.state) : null}
        state={this.state ? this.state.state : null} />
    );
  }
}

export default {
  mount(subscribe) {
    ReactDom.render(<App subscribe={subscribe} />, document.getElementById("app"))
  }
}
