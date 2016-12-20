import * as React from "react";
import PanelLeft from "./PanelLeft";
import PanelCenter from "./PanelCenter";
import PanelRight from "./PanelRight";

const sideWidth = 275;
const centerWidth = window.innerWidth - (sideWidth * 2);

export default class LayoutPanels extends React.Component {
  constructor(props) {
    super(props);
  }

  componentDidMount() {
    $('#ui-layout-container').layout({
      west__size: sideWidth,
      east__size: sideWidth,
      center__onresize: (name, el, state) => {
        this.setState({centerWidth: state.innerWidth})
      }
    })
  }

  render() {
    return (
      <div id="ui-layout-container">
        <div className="ui-layout-west">
          <PanelLeft style={{height: "100%"}} />
        </div>
        <div className="ui-layout-center">
          <PanelCenter className="ui-layout-center"
            width={this.state ? this.state.centerWidth : centerWidth}
            info={this.props.info} />
        </div>
        <div className="ui-layout-east">
          <PanelRight/>
        </div>
      </div>
    );
  }
}
