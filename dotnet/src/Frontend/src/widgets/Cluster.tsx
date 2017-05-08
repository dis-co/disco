import * as React from "react"
import { IDisposable, ILayout, IIris } from "../Interfaces"
import GlobalModel from "../GlobalModel"
import { touchesElement, map, first } from "../Util"
import { showModal } from "../App"
import AddMember from "../modals/AddMember"

declare var IrisLib: IIris;

interface ClusterProps {
  global: GlobalModel
}

class ClusterView extends React.Component<ClusterProps,any> {
  disposables: IDisposable[];
  el: HTMLElement;

  constructor(props) {
    super(props);
  }

  componentDidMount() {
    this.disposables = [];
    this.disposables.push(
      this.props.global.subscribe("project", () => {
        this.forceUpdate();
      })
    );

    this.disposables.push(
      this.props.global.subscribeToEvent("drag", ev => {
        if (this.el != null && ev.model && ev.model.tag === "discovered-service") {
          // console.log("Detected",ev)
          if (touchesElement(this.el, ev.x, ev.y)) {
            switch (ev.type) {
              case "move":
                this.el.classList.add("iris-highlight-blue");
                return;
              case "stop":
                IrisLib.addMember(ev.model);
            }
          }
          this.el.classList.remove("iris-highlight-blue")
        }
      })
    );
  }

  componentWillUnmount() {
    if (Array.isArray(this.disposables)) {
      this.disposables.forEach(x => x.Dispose());
    }
  }

  render() {
    const config = this.props.global.state.project.Config;
    let site = first(config.Sites, (site: any) => site.Id = config.ActiveSite);
    return (
      <div className="iris-cluster"  ref={el => this.el = el}>
        <table className="table is-striped is-narrow" >
          <tfoot>
            <tr><td><a onClick={() => { showModal(AddMember)}}>Add node</a></td></tr>
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
                <tr key={IrisLib.toString(kv[0])}>
                  <td>{node.HostName}</td>
                  <td>{IrisLib.toString(node.IpAddr)}</td>
                  <td>{node.Port}</td>
                  <td>{node.State.ToString()}</td>
                  <td>left</td>
                  <td>Main, VideoPB, Show1</td>
                  <td><a onClick={() => { IrisLib.removeMember(config, kv[0]) }}>Remove</a></td>
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
