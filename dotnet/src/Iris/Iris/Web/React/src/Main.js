import * as React from "react";
import * as ReactDom from "react-dom";
import Layout from "./ColumnLayout";
import { getCurrentSession, login } from "lib";
import overlay from 'muicss/lib/js/overlay';

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
      <Layout
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
