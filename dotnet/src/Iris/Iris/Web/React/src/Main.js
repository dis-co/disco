import * as React from "react";
import * as ReactDom from "react-dom";
import App from "./App";
import SideDrawer from "./SideDrawer";

export default {
  mount(info, subscribe) {
    ReactDom.render(
      <SideDrawer info={info} />,
      document.getElementById("sidedrawer-menu"))

    ReactDom.render(
      <App info={info} subscribe={subscribe} />,
      document.getElementById("app"))
  }
}
