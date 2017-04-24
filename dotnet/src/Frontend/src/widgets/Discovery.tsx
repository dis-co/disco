import * as React from "react"
import { IDisposable, ILayout, IIris } from "../Interfaces"
import GlobalModel from "../GlobalModel"
import { touchesElement, map, first } from "../Util"

declare var Iris: IIris;

interface DiscoveryProps {
  global: GlobalModel
}

class DiscoveryView extends React.Component<DiscoveryProps,any> {
  disposable: IDisposable;

  constructor(props) {
    super(props);
    this.state = { tooltip: {} };
  }

  componentDidMount() {
    this.disposable =
      this.props.global.subscribe("services", () => {
        this.forceUpdate();
      });
  }

  componentWillUnmount() {
    if (this.disposable) {
      this.disposable.dispose();
    }
  }

  displayTooltip(ev: React.MouseEvent<HTMLElement>, info: any) {
    // console.log("left", ev.clientX, "top", ev.clientY)
    this.setState({
      tooltip: {
        visible: true,
        left: ev.clientX,
        top: ev.clientY,
        content: `<p>Host: ${info.hostName}</p><p>IP: ${info.ipAddr}</p><p>Port: ${info.port}</p><p>Web Socket Port: ${info.wsPort}</p><p>Git Port: ${info.gitPort}</p><p>API Port: ${info.apiPort}</p>`
      }
    })
  }

  hideTooltip() {
    this.setState({
      tooltip: {
        visible: false,
      }
    })
  }

  renderService(service) {
    var id = Iris.toString(service.Id)
    var info = {
      tag: "service",
      id: id,
      hostName: service.Hostname,
      ipAddr: Iris.toString(service.IpAddr),
      port: service.Port,
      wsPort: service.WsPort,
      gitPort: service.GitPort,
      apiPort: service.ApiPort
    }
    return (<div key={id} className="iris-discovered-service"
      onMouseEnter={ev => this.displayTooltip(ev, info)}
      onMouseLeave={() => this.hideTooltip()}
    >{id}</div>)
  }  

  render() {
    const tooltip = this.state.tooltip;
    const services =
      //this.props.global.state.services;
      mockupServices;
    return (
      <div className="iris-discovery">
        <div className="iris-tooltip" style={{
          display: tooltip.visible ? "block" : "none",
          left: tooltip.left,
          top: tooltip.right
        }} dangerouslySetInnerHTML={{__html: tooltip.content}}></div>
        {map(services, x => this.renderService(x))}
      </div>
    )
  }
}

class MockupService {
  constructor(public Id: string, public Hostname = "localhost", public IpAddr = "192.127.0.1", public Port = 1100, public WsPort = 1200, public GitPort = 1300, public ApiPort = 1400) {
  }
}

const mockupServices = [
  new MockupService("Service 1"),
  new MockupService("Service 2"),
  new MockupService("Service 3")
];

export default class Discovery {
  view: typeof DiscoveryView;
  name: string;
  layout: ILayout;

  constructor() {
    this.view = DiscoveryView;
    this.name = "Discovered Services";
    this.layout = {
      x: 0, y: 0,
      w: 8, h: 5,
      minW: 2, maxW: 10,
      minH: 1, maxH: 10
    };
  }
}
