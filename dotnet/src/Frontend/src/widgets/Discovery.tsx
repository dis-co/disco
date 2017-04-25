import * as React from "react"
import { IDisposable, ILayout, IIris } from "../Interfaces"
import GlobalModel from "../GlobalModel"
import domtoimage from "dom-to-image"
import { touchesElement, map, first } from "../Util"

declare var Iris: IIris;

interface DiscoveryProps {
  global: GlobalModel
}

class DiscoveryView extends React.Component<DiscoveryProps,any> {
  disposable: IDisposable;
  childNodes: Map<string, HTMLElement>;

  constructor(props) {
    super(props);
    this.state = { tooltip: {} };
    this.childNodes = new Map();
  }

  startDragging(id, model) {
    const __this = this;
    const node = __this.childNodes.get(id);
    if (node == null) { return; }

    domtoimage.toPng(node)
      .then(dataUrl => {
        // console.log("drag start")
        const img = $("#iris-drag-image").attr("src", dataUrl).css({display: "block"});
        $(document)
          .on("mousemove.drag", e => {
            // console.log("drag move", {x: e.clientX, y: e.clientY})
            $(img).css({left:e.pageX, top:e.pageY});
            __this.props.global.triggerEvent("drag", {
              type: "move",
              model: model,
              x: e.clientX,
              y: e.clientY
            });
          })
          .on("mouseup.drag", e => {
            // console.log("drag stop")
            img.css({display: "none"});
            __this.props.global.triggerEvent("drag", {
              type: "stop",
              model: model,
              x: e.clientX,
              y: e.clientY,
            });
            $(document).off("mousemove.drag mouseup.drag");
          })
      })
      .catch(error => {
          console.error('Error when generating image:', error);
      });
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
      tag: "discovered-service",
      id: id,
      hostName: service.Hostname,
      ipAddr: Iris.toString(service.IpAddr),
      port: service.Port,
      wsPort: service.WsPort,
      gitPort: service.GitPort,
      apiPort: service.ApiPort
    }
    return (<div
      key={id}
      className="iris-discovered-service"
      ref={el => { if (el != null) this.childNodes.set(id, el) }}
      onMouseEnter={ev => this.displayTooltip(ev, info)}
      onMouseLeave={() => this.hideTooltip()}
      onMouseDown={() => this.startDragging(id, info)}
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
