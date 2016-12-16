import * as React from "react";
import PanelLeft from "./PanelLeft";
import PanelCenter from "./PanelCenter";
import PanelRight from "./PanelRight";

export default class LayoutPanels extends React.Component {
  constructor(props) {
    super(props);
  }

  componentDidMount() {
     $('ui-layout-container').layout({ applyDefaultStyles: true })
  }

  render() {
    return (
      <div id="ui-layout-container">
        <PanelLeft className="ui-layout-east" />
        <PanelCenter className="ui-layout-center" info={this.props.info} />
        <PanelRight className="ui-layout-west" />
      </div>
    );
  }
}
