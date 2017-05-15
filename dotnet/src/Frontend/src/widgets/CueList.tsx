import * as React from "react"
import Spread, { SpreadView } from "./Spread"
import Clock from "./Clock"
import domtoimage from "dom-to-image"
import { touchesElement, map } from "../Util"
import { IDisposable, ILayout, IIris } from "../Interfaces"
import GlobalModel from "../GlobalModel"

declare var IrisLib: IIris;

interface CueProps {
  id: number
  model: CueList
  global: GlobalModel
}

class CueListView extends React.Component<CueProps,any> {
  disposables: IDisposable[];
  el: HTMLElement;

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
                this.props.model.cues.push(new Cue(ev.model));
                this.forceUpdate();
            }
          }
          this.el.classList.remove("iris-highlight-blue")
        }
      })
    );
  }

  componentWillUnmount() {
    if (Array.isArray(this.disposables)) {
      this.disposables.forEach(x => x.dispose());
    }
  }

  updateSource() {
    this.props.model.cues.forEach(cue => cue.updateSource());
  }

  render() {
    return (
      <div className="iris-cuelist" ref={el => this.el = el}>
        <div className="level">
          <div className="level-left">
            <button className="button level-item"
                    style={{margin: 5}}
                    onClick={ev => this.updateSource()}>
              Fire!
            </button>
          </div>
          <div className="level-right">
            <div className="level-item">
              <Clock global={this.props.global} />
            </div>
          </div>
        </div>
        {map(this.props.model.cues, (cue, i) => {
          return (
            <div key={i}>
              {React.createElement(SpreadView as any, {model:cue, global: this.props.global})}
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

class Cue {
  pin: any;
  open: boolean;
  rows: [string, any][];
  updateView: boolean;

  constructor(spread: Spread) {
    this.pin = spread.pin;
    this.open = false;
    this.rows = spread.rows;
    this.updateView = true;
  }

  update(rowIndex, newValue) {
    this.rows[rowIndex][1] = newValue;
  }

  updateSource() {
    for (let i = 0; i < this.rows.length; i++) {
      IrisLib.updateSlices(this.pin, i, this.rows[i][1]);
    }
  }
}
