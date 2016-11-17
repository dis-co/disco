import * as React from "react";
import * as ReactDom from "react-dom";

import { add } from "./tests.ts"

class App extends React.Component {
  render() {
    return <h1>Hello Iris! {add(5,5) }</h1>
  }
}

export default {
  mount(subscribe) {
    ReactDom.render(<App subscribe={subscribe} />, document.getElementById("app"))
  }
}
