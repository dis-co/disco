import * as React from "react";
import * as ReactDom from "react-dom";
import LoginDialog from "./LoginDialog";
import Layout from "./Layout";
import { getCurrentSession, login } from "lib";


function getSession(state) {
  if (state) {
    return getCurrentSession(state);
  }
  return null;
}

class App extends React.Component {
  constructor(props) {
    super(props);
    props.subscribe(state => {
      console.log(state);
      this.setState(state);
    })
  }

  render() {
    return (
      <LoginDialog
        login={(username, password) => login(this.state, username, password)}
        session={getSession(this.state)} />
    );
  }
}

export default {
  mount(subscribe) {
    ReactDom.render(<App subscribe={subscribe} />, document.getElementById("app"))
  }
}
