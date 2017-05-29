import React, { Component } from 'react'
import { Spread, SpreadView } from "../../fable/Frontend/Widgets/Spread.fs"
import domtoimage from "dom-to-image"
import { touchesElement, map } from "../Util.ts"

class View extends Component {
  constructor(props) {
    super(props);
    this.childNodes = new Map();
  }

  startDragging(key, pin) {
    const _this = this;
    const node = _this.childNodes.get(key);
    if (node == null) { return; }

    domtoimage.toPng(node)
      .then(dataUrl => {
        // console.log("drag start")
        const img = $("#iris-drag-image").attr("src", dataUrl).css({display: "block"});
        $(document)
          .on("mousemove.drag", e => {
            // console.log("drag move", {x: e.clientX, y: e.clientY})
            $(img).css({left:e.pageX, top:e.pageY});
            _this.props.global.triggerEvent("drag", {
              type: "move",
              model: pin,
              origin: _this.props.id,
              x: e.clientX,
              y: e.clientY
            });
          })
          .on("mouseup.drag", e => {
            // console.log("drag stop")
            img.css({display: "none"});
            _this.props.global.triggerEvent("drag", {
              type: "stop",
              model: pin,
              origin: _this.props.id,
              x: e.clientX,
              y: e.clientY
            });
            $(document).off("mousemove.drag mouseup.drag");
          })
      })
      .catch(error => {
          console.error('Error when generating image:', error);
      });
  }

  componentDidMount() {
    this.disposables = [];

    this.disposables.push(
      this.props.global.subscribe(["pinGroups", "useRightClick"], () => {
        this.forceUpdate();
      })
    );
  }

  componentWillUnmount() {
    if (Array.isArray(this.disposables)) {
      this.disposables.forEach(x => x.dispose());
    }
  }

  render() {
    return (
      <div className="iris-grapview" ref={el => this.el = el}>
        {map(this.props.global.state.pinGroups, (pinGroup, i) => (
          <div key={i} className="iris-pingroup">
            <h3 className="title is-3">{pinGroup[1].Name}</h3>
            <div>
              {map(pinGroup[1].Pins, (kv,i) => {
                const pin = kv[1], key = IrisLib.toString(pin.Id);
                return <SpreadView
                  key={key}
                  ref={el => { if (el != null) this.childNodes.set(key, el) }}
                  pin={pin}
                  global={this.props.global}
                  onDragStart={() => this.startDragging(key, pin)} />
              })}
            </div>
          </div>
        ))}
      </div>
    )
  }
}

export default class GraphView {
  constructor() {
    this.view = View;
    this.name = "Graph View";
    this.layout = {
      x: 0, y: 0,
      w: 8, h: 5,
      minW: 2, maxW: 10,
      minH: 1, maxH: 10
    };
  }
}
