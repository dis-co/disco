import * as React from "react"
import Spread from "./Spread"
import domtoimage from "dom-to-image"
import { touchesElement, map } from "../Util"
import { IDisposable, ILayout } from "../Interfaces"
import GlobalModel from "../GlobalModel"

interface CueProps {
  id: number
  model: CueList
  global: GlobalModel
}

class CueListView extends React.Component<CueProps,{}> {
  disposables: IDisposable[];
  el: any;

  constructor(props: CueProps) {
    super(props);
  }

  componentDidMount() {
    this.disposables = [];

    this.disposables.push(
      this.props.global.subscribeToEvent("drag", ev => {
        if (this.el != null && ev.origin !== this.props.id) {
          // console.log("Detected",ev)
          if (touchesElement(this.el, ev.x, ev.y)) {
            switch (ev.type) {
              case "move":
                this.el.classList.add("iris-highlight-blue");
                return;
              case "stop":
                this.props.model.cues.push(ev.model);
                this.forceUpdate();
            }
          }
          this.el.classList.remove("iris-highlight-blue")
        }
      })
    );

    this.disposables.push(
      this.props.global.subscribe("clock", () => this.forceUpdate())
    );
  }

  componentWillUnmount() {
    if (Array.isArray(this.disposables)) {
      this.disposables.forEach(x => x.dispose());
    }
  }  

  render() {
    return (
      <div className="iris-cuelist" ref={el => this.el = el}>
        <span>Frames: {this.props.global.state.clock}</span>
        {map(this.props.model.cues, (cue, i) => {
          const View = cue.view as any;
          return (
            <div key={i}>
              <View
                model={cue}
                global={this.props.global} />
            </div>
          )})}
      </div>
    )
  }
}

export default class CueList {
  view: typeof CueListView;
  name: string;
  layout: ILayout;
  cues: any[];

  constructor() {
    this.view = CueListView;
    this.name = "Cue List";
    this.layout = {
      x: 0, y: 0,
      w: 8, h: 5,
      minW: 2, maxW: 10,
      minH: 1, maxH: 10
    };
    this.cues = [];
  }
}
