import * as React from "react";
import * as ReactDom from "react-dom";
import LayoutColumn from "./LayoutColumn";
import ModalDialog from "./ModalDialog";
import { getCurrentSession } from 'iris';
import { STATUS, MODALS, SKIP_LOGIN } from './Constants';

let modal = null;
let initInfo = null;

export function showModal(content, onSubmit) {
  modal.setState({ content, onSubmit });
}

class App extends React.Component {
  constructor(props) {
    super(props);
    props.subscribe(info => {
      if (SKIP_LOGIN) {
        console.log(info);
        this.setState(info);
        return;
      }

      const status = info.session.Status.StatusType.ToString();
      switch (status) {
        case STATUS.AUTHORIZED:
          this.setState(info);
          break;
        case STATUS.UNAUTHORIZED:
          this.setState(initInfo);
          showModal(MODALS.LOGIN);
          break;
      }
    })
  }

  render() {
    const info = this.state || this.props.info;
    return (
      <div className="column-layout-wrapper">
        <ModalDialog info={info} ref={el => modal = (el || modal)} />
        <LayoutColumn info={info} />;
      </div>
    )
  }
}

export default {
  mount(info, subscribe) {
    initInfo = info;
    ReactDom.render(
      <App info={info} subscribe={subscribe} />,
      document.getElementById("app"))
  }
}
