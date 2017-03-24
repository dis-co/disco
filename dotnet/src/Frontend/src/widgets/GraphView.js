import React, { Component } from 'react'
import Spread from "./Spread"
import IOBox from "./IOBox"
import domtoimage from "dom-to-image"
import { touchesElement, map } from "../Util.ts"

class View extends Component {
  constructor(props) {
    super(props);
    this.childNodes = new Map();
  }

  startDragging(model, index) {
    const _this = this;
    const node = _this.childNodes.get(index);
    if (node == null) { return; }

    domtoimage.toPng(node)
      .then(dataUrl => {
        console.log("drag start")
        const img = $("#iris-drag-image").attr("src", dataUrl).css({display: "block"});
        $(document)
          .on("mousemove.drag", e => {
            $(img).css({left:e.pageX, top:e.pageY});
            _this.props.global.triggerEvent("drag", {
              type: "move",
              model: model,
              origin: _this.props.id,
              x: e.clientX,
              y: e.clientY
            });
          })
          .on("mouseup.drag", e => {
            console.log("drag stop")
            img.css({display: "none"});
            _this.props.global.triggerEvent("drag", {
              type: "stop",
              model: model,
              origin: _this.props.id,
              x: e.clientX,
              y: e.clientY,
              removeModelFromOrigin() {
                _this.props.model.elements.splice(index, 1);
                _this.forceUpdate();
              }
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
      this.props.global.subscribeToEvent("drag", ev => {
        if (this.el != null && ev.origin !== this.props.id) {
          if (touchesElement(this.el, ev.x, ev.y)) {
            switch (ev.type) {
              case "move":
                this.el.classList.add("iris-highlight-blue");
                return;
              case "stop":
                ev.removeModelFromOrigin();
                this.props.model.elements.push(ev.model);
                this.forceUpdate();
            }
          }
          this.el.classList.remove("iris-highlight-blue")
        }
      })
    );

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
            <p>{pinGroup[1].Name}</p>
            {map(pinGroup[1].Pins, (pin,i) => {
              var model = new Spread(pin[1]);
              const View = model.view;
              return (
                <div key={i}
                  ref={el => { if (el != null) this.childNodes.set(i, el.childNodes[0]) }}>
                  <View
                    model={model}
                    global={this.props.global}
                    onDragStart={() => this.startDragging(model, i)} />
                </div>
              )})}
          </div>
        ))}
      </div>          
    )
  }
}

export default class Manager {
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
