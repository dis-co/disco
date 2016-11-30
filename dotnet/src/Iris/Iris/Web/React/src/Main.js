import * as React from "react";
import * as ReactDom from "react-dom";
import LayoutColumn from "./LayoutColumn";

class App extends React.Component {
  constructor(props) {
    super(props);
    props.subscribe(info => {
      // console.log("Received state:",info.state);
      this.setState(info);
    })
  }

  render() {
    return <LayoutColumn info={this.state || this.props.info} />;
  }
}

export default {
  mount(info, subscribe) {
    ReactDom.render(
      <App info={info} subscribe={subscribe} />,
      document.getElementById("app"))
  }
}
