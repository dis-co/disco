import * as React from "react";
// import LayoutColumn from "./LayoutColumn";
import LayoutPanels from "./LayoutPanels";
import ModalDialog from "./ModalDialog";
import { getCurrentSession } from 'iris';
import { STATUS, MODALS, SKIP_LOGIN } from './Constants';

let modal = null;
let initInfo = null;

export function showModal(content, onSubmit) {
  modal.setState({ content, onSubmit });
}

export default class App extends React.Component {
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
    let info = this.state;
    if (info == null) {
      info = this.props.info;
      initInfo = info;
    }
    return (
      <div className="column-layout-wrapper">
        <ModalDialog info={info} ref={el => modal = (el || modal)} />
        <LayoutPanels info={info} />;
      </div>
    )
  }
}
