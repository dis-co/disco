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
      return <table className="iris-table"></table>
    }
    const config = this.props.global.state.project.Config;
    const site = tryFirst(config.Sites, site => site.Id = config.ActiveSite);
    const padding5 = {paddingLeft: "5px"};
    const topBorder ={borderTop: "1px solid lightgray"}
    return (
      <table className="iris-table">
        <thead>
          <tr>
            <th className="width-20" style={padding5}>Host</th>
            <th className="width-15">IP</th>
            <th className="width-25"></th>
            <th className="width-15"></th>
            <th className="width-15"></th>
            <th className="width-10"></th>
          </tr>
        </thead>
        <tbody>
          {map(site != null ? site.Members : [], kv => {
            const node = kv[1];
            return (
              <tr key={IrisLib.toString(kv[0])}>
                <td className="width-20" style={Object.assign({},padding5,topBorder)}>
                  <span className="iris-output iris-icon icon-host">{node.HostName} <span className="iris-icon icon-bull iris-status-off" /></span>
                </td>
                <td className="width-15" style={topBorder}>{IrisLib.toString(node.IpAddr)}</td>
                <td className="width-25" style={topBorder}>{node.Port}</td>
                <td className="width-15" style={topBorder}>{node.State.ToString()}</td>
                <td className="width-15" style={topBorder}>shortkey</td>
                <td className="width-10" style={topBorder}><button className="iris-button iris-icon icon-autocall" /></td>
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
      <button className="iris-button" onClick={() => { showModal(AddMember)}}>Add member</button>;
    this.layout = {
      x: 0, y: 0,
      w: 8, h: 5,
      minW: 4, maxW: 10,
      minH: 1, maxH: 10
    };
  }
}
