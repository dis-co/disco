import * as React from "react";
import * as ReactDom from "react-dom";
import LoginDialog from "./LoginDialog";
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
    var session = getSession(this.state);

      // <LoginDialog
      //   login={(username, password) => login(this.state, username, password)}
      //   session={session} />

    return <Layout />;
  }
}

export default {
  mount(subscribe) {
    ReactDom.render(<App subscribe={subscribe} />, document.getElementById("app"))
  }
}
