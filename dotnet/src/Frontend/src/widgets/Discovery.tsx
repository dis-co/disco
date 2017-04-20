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
    return (<tr key={id}><td><div style={{background:"red"}} >{id.substr(0, 4) + "..."}</div></td></tr>)
  }  

  render() {
    return (
      <div className="iris-discovery">
        <table className="table is-striped is-narrow" >      
          <thead>
            <tr><td>Services</td></tr>
          </thead>
          <tbody>
            {map(this.props.global.state.services, x => this.renderService(x))}
          </tbody>
        </table>        
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
    this.name = "Discovery";
    this.layout = {
      x: 0, y: 0,
      w: 8, h: 5,
      minW: 2, maxW: 10,
      minH: 1, maxH: 10
    };
  }
}
