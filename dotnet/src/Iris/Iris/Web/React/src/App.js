import * as React from "react";
import PanelLeft from "./PanelLeft";
import PanelCenter from "./PanelCenter";
import PanelRight from "./PanelRight";
import ModalDialog from "./ModalDialog";
import { getCurrentSession } from 'iris';
import { SKIP_LOGIN } from './Constants';
import LOGIN from "./modals/Login";

let modal = null;
let initInfo = null;

const sideInitWidth = 275;

export function showModal(content, onSubmit) {
  modal.setState({ content, onSubmit });
}

export default class App extends React.Component {
  constructor(props) {
    super(props);
    this.state = {};
    props.subscribe(info => {
      this.setState({info: info});
    })
  }

  componentDidMount() {
    $('#ui-layout-container')
      .layout({
        west__size: sideInitWidth,
        east__size: sideInitWidth,
        // center__onresize: (name, el, state) => {
        //   this.setState({centerWidth: state.innerWidth})
        // }
    })
  }

  render() {
    let info = this.state.info;
    if (info == null) {
      info = this.props.info;
      initInfo = info;
    }
    let centerWidth = this.state.centerWidth;
    if (centerWidth == null) {
      centerWidth = $("#app").innerWidth() - (sideInitWidth * 2);
    }
    return (
      <div id="ui-layout-container">
        <ModalDialog info={info} ref={el => modal = (el || modal)} />
        <div className="ui-layout-west">
          <PanelLeft info={info} />
        </div>
        <div className="ui-layout-center">
          <PanelCenter className="ui-layout-center"
            width={centerWidth}
            info={info} />
        </div>
        <div className="ui-layout-east">
          <PanelRight info={info}/>
        </div>
      </div>
    )
  }
}
