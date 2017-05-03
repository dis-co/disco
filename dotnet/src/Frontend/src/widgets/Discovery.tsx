import * as React from "react"
import { IDisposable, ILayout, IIris } from "../Interfaces"
import GlobalModel from "../GlobalModel"
import domtoimage from "dom-to-image"
import { touchesElement, map, first } from "../Util"

declare var IrisLib: IIris;

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
        content: `<p>Host: ${info.hostName}</p><p>IP: ${info.ipAddr}</p><p>Port: ${info.port}</p><p>HTTP Port: ${info.httpPort}</p><p>Web Socket Port: ${info.wsPort}</p><p>Git Port: ${info.gitPort}</p><p>API Port: ${info.apiPort}</p>`
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
    var id = IrisLib.toString(service.Id);
    var ipAddr = "0.0.0.0", port = 0, wsPort = 0, httpPort = 0, gitPort = 0, apiPort = 0;
    if (service.AddressList.length > 0) {
        ipAddr = IrisLib.toString(service.AddressList[0]);
    }
    for (let i = 0; i < service.Services.length; i++) {
      let exposed = service.Services[i];
      switch (IrisLib.toString(exposed.ServiceType)) {
        case "git":
          gitPort = exposed.Port;
          break;
        case "raft":
          port = exposed.Port;
          break;
        case "api":
          apiPort = exposed.Port;
          break;
        case "http":
          httpPort = exposed.Port;
          break;
        case "ws":
          wsPort = exposed.Port;
          break;
      }
    }
    var info = {
      tag: "discovered-service",
      id: id,
      hostName: service.HostName,
      ipAddr: ipAddr,
      port: port,
      wsPort: wsPort,
      httpPort: httpPort,
      gitPort: gitPort,
      apiPort: apiPort,
      enabled: IrisLib.toString(service.Status) === "idle"
    }
    var props = {
      key: id,
      className: "iris-discovered-service",
      ref: el => { if (el != null) this.childNodes.set(id, el) },
      onMouseEnter: ev => this.displayTooltip(ev, info),
      onMouseLeave: () => this.hideTooltip(),
    };
    if (info.enabled) {
      Object.assign(props, {
        className: "iris-discovered-service enabled",
        onMouseDown: () => this.startDragging(id, info)
      });
    }
    return (<div {...props}>{service.Name || id}</div>)
  }  

  render() {
    const tooltip = this.state.tooltip;
    const services = this.props.global.state.services;
    return (
      <div className="iris-discovery">
        <div className="iris-tooltip" style={{
          display: tooltip.visible ? "block" : "none",
          left: tooltip.left,
          top: tooltip.right
        }} dangerouslySetInnerHTML={{__html: tooltip.content}}></div>
        {map(services, kv => this.renderService(kv[1]))}
      </div>
    )
  }
}

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
