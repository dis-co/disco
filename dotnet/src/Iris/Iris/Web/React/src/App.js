import * as React from "react";
import PanelLeft from "./PanelLeft";
import PanelCenter from "./PanelCenter";
import PanelRight from "./PanelRight";
import ModalDialog from "./ModalDialog";
import { getCurrentSession } from 'iris';
import { STATUS, MODALS, SKIP_LOGIN } from './Constants';

let modal = null;
let initInfo = null;

const sideInitWidth = 275;
const centerInitWidth = window.innerWidth - (sideInitWidth * 2);

export function showModal(content, onSubmit) {
  modal.setState({ content, onSubmit });
}

export default class App extends React.Component {
  constructor(props) {
    super(props);
    this.state = {};
    props.subscribe(info => {
      if (SKIP_LOGIN) {
        console.log(info);
        this.setState({info: info});
        return;
      }
      const status = info.session.Status.StatusType.ToString();
      switch (status) {
        case STATUS.AUTHORIZED:
          this.setState({info: info});
          break;
        case STATUS.UNAUTHORIZED:
          this.setState({info: initInfo});
          showModal(MODALS.LOGIN);
          break;
      }
    })
  }

  componentDidMount() {
    $('#ui-layout-container').layout({
      west__size: sideInitWidth,
      east__size: sideInitWidth,
      center__onresize: (name, el, state) => {
        this.setState({centerWidth: state.innerWidth})
      }
    })
  }

  render() {
    let info = this.state.info;
    if (info == null) {
      info = this.props.info;
      initInfo = info;
    }
    return (
      <div id="ui-layout-container">
        <ModalDialog info={info} ref={el => modal = (el || modal)} />
        <div className="ui-layout-west">
          <PanelLeft info={this.props.info} />
        </div>
        <div className="ui-layout-center">
          <PanelCenter className="ui-layout-center"
            width={this.state.centerWidth || centerInitWidth}
            info={this.props.info} />
        </div>
        <div className="ui-layout-east">
          <PanelRight info={this.props.info}/>
        </div>
      </div>
    )
  }
}
