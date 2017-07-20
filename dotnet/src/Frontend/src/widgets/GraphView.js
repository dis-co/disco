// Change to triple-slash comment to activate TS check
/* @ts-check */

import * as React from 'react'
import { PinView } from "../../fable/Frontend/Widgets/PinView.fs"
import domtoimage from "dom-to-image"
import { touchesElement, map, jQueryEventAsPromise, raceIndexed } from "../Util"

class View extends React.Component {
  constructor(props) {
    super(props);
    this.childNodes = new Map();
  }

  continueDragging(dataUrl, pin) {
    const __this = this;
    // console.log("drag start")
    const img = $("#iris-drag-image").attr("src", dataUrl).css({display: "block"});
    $(document)
      .on("mousemove.drag", e => {
        // console.log("drag move", {x: e.clientX, y: e.clientY})
        $(img).css({left:e.pageX, top:e.pageY});
        __this.props.global.triggerEvent("drag", {
          type: "move",
          model: pin,
          origin: __this.props.id,
          x: e.clientX,
          y: e.clientY
        });
      })
      .on("mouseup.drag", e => {
        // console.log("drag stop")
        img.css({display: "none"});
        __this.props.global.triggerEvent("drag", {
          type: "stop",
          model: pin,
          origin: __this.props.id,
          x: e.clientX,
          y: e.clientY
        });
        $(document).off("mousemove.drag mouseup.drag");
      })
  }

  startDragging(key, pin) {
    const node = this.childNodes.get(key);
    if (node == null) { return; }

    var date1 = new Date();
    raceIndexed(domtoimage.toPng(node, {}), jQueryEventAsPromise(document, "mouseup.domtoimage"))
    .then(([i, data]) => {
      if (i === 0) {
        this.continueDragging(data, pin);
      }
      // If the mouseup event happens before the image is finished, do nothing
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
      <ul className="iris-graphview" ref={el => this.el = el}>
        {map(this.props.global.state.pinGroups, (pinGroup, i) => (
          <li key={i}>
            <div>{pinGroup[1].Name}</div>
            {map(pinGroup[1].Pins, (kv,i) => {
              const pin = kv[1], key = IrisLib.toString(pin.Id);
              return (
                // We need to wrap the PinView in a div to get the actual HTML element in `ref`
                <div key={key} ref={el => { if (el != null) this.childNodes.set(key, el.childNodes[0]) }}>
                  <PinView
                    key={key}
                    pin={pin}
                    global={this.props.global}
                    onDragStart={() => this.startDragging(key, pin)} />
                </div>
              )
            })}
        </li>
      ))}
      </ul>
    )
  }
}
{/*<div className="iris-graphview" ref={el => this.el = el}>
  {map(this.props.global.state.pinGroups, (pinGroup, i) => (
    <div key={i} className="iris-pingroup">
      <div className="iris-pingroup-name">{pinGroup[1].Name + ":"}</div>
      {map(pinGroup[1].Pins, (kv,i) => {
        const pin = kv[1], key = IrisLib.toString(pin.Id);
        return (
          // We need to wrap the PinView in a div to get the actual HTML element in `ref`
          <div key={key} ref={el => { if (el != null) this.childNodes.set(key, el.childNodes[0]) }}>
            <PinView
              key={key}
              pin={pin}
              global={this.props.global}
              onDragStart={() => this.startDragging(key, pin)} />
          </div>
        )
      })}
    </div>
  ))}
</div>*/}

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
