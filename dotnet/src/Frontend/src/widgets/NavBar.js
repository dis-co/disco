import * as React from "react";
import SkyLight from 'react-skylight';
import * as util from "../Util";
import CreateProject from '../modals/CreateProject';

let navBarInstance = null;

export function showModal(content, props) {
  if (navBarInstance == null) {
      throw new Error("No NavBar instance available");
  }
  return navBarInstance.showModal(content, props);
}

export default class NavBar extends React.Component {
  constructor(props) {
    super(props);
    this.state = {
        modal: null,
        isDropdownOpen: false,
        serviceInfo: props.global.state.serviceInfo,
        useRightClick: props.global.state.useRightClick
    };
  }

  componentDidMount() {
    navBarInstance = this;
    this.disposable =
        this.props.global.subscribe("serviceInfo", serviceInfo =>
            this.setState({serviceInfo}));
  }

  componentWillUnmount() {
    navBarInstance = null;
    if (this.disposable) {
      this.disposable.dispose();
    }
  }

  onClick(dropdownIndex) {
    return () => {
      switch (dropdownIndex) {
          case 0:
            showModal(CreateProject);
            break;
          case 1:
            util.loadProject();
            break;
          case 2:
            IrisLib.saveProject();
            break;
          case 3:
            IrisLib.unloadProject();
            break;
          case 4:
            IrisLib.shutdown();
            break;
          case 5:
            var useRightClick = !this.state.useRightClick;
            this.props.global.useRightClick(useRightClick);
            this.setState({useRightClick});
            break;
      }
    }
  }

  showModal(content, props) {
    return new Promise(resolve =>
        this.setState({
            modal: {
                content,
                props: props || {},
                onSubmit: resolve
            }
        }, () => this.modalEl.show())
    );
  }

  renderModal() {
    if (this.state.modal == null) {
        return null;
    }

    return React.createElement(this.state.modal.content, {
      onSubmit: values => {
        this.modalEl.hide();
        this.state.modal.onSubmit(values);
      },
      ...this.state.modal.props
    });
  }

  render() {
    return (
      <div>
        <SkyLight
            hideOnOverlayClicked
            ref={el => this.modalEl = el}
            title={this.state.modal ? this.state.modal.title : ""}
            dialogStyles={{height: "inherit"}}>
            {this.renderModal()}
        </SkyLight>
        <nav id="app-header" className="nav">
            <div className="nav-left">
                <h1 className="nav-item">Iris</h1>
                <a className="nav-item" onClick={() => this.setState({isDropdownOpen: !this.state.isDropdownOpen})}>Menu</a>
                {this.state.isDropdownOpen ?
                    <div className="box dropdown">
                    <ul>
                        <li><a onClick={this.onClick(0).bind(this)}>Create Project</a></li>
                        <li><a onClick={this.onClick(1).bind(this)}>Load Project</a></li>
                        <li><a onClick={this.onClick(2).bind(this)}>Save Project</a></li>
                        <li><a onClick={this.onClick(3).bind(this)}>Unload Project</a></li>
                        <li><a onClick={this.onClick(4).bind(this)}>Shutdown</a></li>
                        <li><a onClick={this.onClick(5).bind(this)}>{"Use right click: " + this.state.useRightClick}</a></li>
                    </ul>
                    </div>
                    : null}
            </div>
            <div className="nav-right">
                <span className="nav-item">Iris v{this.state.serviceInfo.version} - build {this.state.serviceInfo.buildNumber}</span>
            </div>
        </nav>
      </div>
    )
  }
}
