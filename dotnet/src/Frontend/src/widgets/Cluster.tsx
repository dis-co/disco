import * as React from "react"
import { IDisposable, ILayout, IIris } from "../Interfaces"
import GlobalModel from "../GlobalModel"
import { touchesElement, map, first } from "../Util"
import { showModal } from "../App"
import AddNode from "../modals/AddNode"

declare var Iris: IIris;

interface ClusterProps {
  global: GlobalModel
}

class ClusterView extends React.Component<ClusterProps,any> {
  disposable: IDisposable;

  constructor(props) {
    super(props);
  }

  componentDidMount() {
    this.disposable =
      this.props.global.subscribe("project", () => {
        this.forceUpdate();
      });
  }

  componentWillUnmount() {
    if (this.disposable) {
      this.disposable.dispose();
    }
  }

  render() {
    const config = this.props.global.state.project.Config;
    let site = first(config.Sites, (site: any) => site.Id = config.ActiveSite);
    return (
      <div className="iris-cluster">
        <table className="table is-striped is-narrow" >
          <tfoot>
            <tr><td><a onClick={() => { showModal(AddNode)}}>Add node</a></td></tr>
          </tfoot>          
          <thead>
            <tr>
              <th>Host</th>
              <th>IP</th>
              <th>Port</th>
              <th>State</th>
              <th>Role</th>
              <th>Tags</th>
            </tr>
          </thead>
          <tbody>
            {map(site.Members, kv => {
              const node = kv[1];
              return (
                <tr key={kv[0].Fields[0]}>
                  <td>{node.HostName}</td>
                  <td>{node.IpAddr.Fields[0]}</td>
                  <td>{node.Port}</td>
                  <td>{node.State.ToString()}</td>
                  <td>left</td>
                  <td>Main, VideoPB, Show1</td>
                  <td><a onClick={() => { Iris.removeMember(config, kv[0]) }}>Remove</a></td>
                </tr>
              );
            })}
          </tbody>
        </table>        
      </div>
    )
  }
}

export default class Cluster {
  view: typeof ClusterView;
  name: string;
  layout: ILayout;

  constructor() {
    this.view = ClusterView;
    this.name = "Cluster";
    this.layout = {
      x: 0, y: 0,
      w: 8, h: 5,
      minW: 2, maxW: 10,
      minH: 1, maxH: 10
    };
  }
}
