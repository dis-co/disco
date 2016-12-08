import * as React from "react";
import * as ReactDom from "react-dom";
import App from "./App";

export default {
  mount(info, subscribe) {
    ReactDom.render(
      <App info={info} subscribe={subscribe} />,
      document.getElementById("app"))
  }
}
