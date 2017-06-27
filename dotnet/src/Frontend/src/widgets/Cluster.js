import * as React from "react"
import { IDisposable, ILayout, IIris } from "../Interfaces"
import { GlobalModel } from "../../fable/Frontend/GlobalModel.fs"
import { touchesElement, map, tryFirst } from "../Util"
import { showModal } from "../App"
import AddMember from "../modals/AddMember"

class ClusterView extends React.Component {
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

    // this.disposables.push(
    //   this.props.global.subscribeToEvent("drag", ev => {
    //     if (this.el != null && ev.model && ev.model.tag === "discovered-service") {
    //       // console.log("Detected",ev)
    //       if (touchesElement(this.el, ev.x, ev.y)) {
    //         switch (ev.type) {
    //           case "move":
    //             this.el.classList.add("iris-highlight-blue");
    //             return;
    //           case "stop":
    //             IrisLib.addMember(ev.model);
    //         }
    //       }
    //       this.el.classList.remove("iris-highlight-blue")
    //     }
    //   })
    // );
  }

  componentWillUnmount() {
    if (Array.isArray(this.disposables)) {
      this.disposables.forEach(x => x.dispose());
    }
  }

  render() {
    if (this.props.global.state.project == null) {
      return <table className="iris-list iris-cluster"></table>
    }
    const config = this.props.global.state.project.Config;
    let site = tryFirst(config.Sites, site => site.Id = config.ActiveSite);
    return (
      <table className="iris-list iris-cluster">
        <thead>
          <tr className="iris-list-label-row">
            <th className="p1 iris-list-label">Host</th>
            <th className="p2 iris-list-label">IP</th>
            <th className="p3 iris-list-label"></th>
            <th className="p4 iris-list-label"></th>
            <th className="p5 iris-list-label"></th>
            <th className="p6 iris-list-label"></th>
          </tr>
        </thead>
        <tbody>
          {map(site != null ? site.Members : [], kv => {
            const node = kv[1];
            return (
              <tr key={IrisLib.toString(kv[0])}>
                <td className="p1"><span className="iris-output iris-icon icon-host">{node.HostName} <span className="iris-icon icon-bull iris-status-off" /></span></td>
                <td className="p2">{IrisLib.toString(node.IpAddr)}</td>
                <td className="p3">{node.Port}</td>
                <td className="p4">{node.State.ToString()}</td>
                <td className="p5">shortkey</td>
                <td className="p6"><button className="iris-icon icon-autocall" /></td>
              </tr>
            );
          })}
        </tbody>
      </table>
    )
  }
}

export default class Cluster {
  constructor() {
    this.view = ClusterView;
    this.name = "Cluster";
    this.titleBar =
      <button onClick={() => { showModal(AddMember)}}>Add member</button>;
    this.layout = {
      x: 0, y: 0,
      w: 8, h: 5,
      minW: 2, maxW: 10,
      minH: 1, maxH: 10
    };
  }
}
