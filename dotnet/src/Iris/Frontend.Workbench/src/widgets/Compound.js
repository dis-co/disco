import React, { Component } from 'react'
import Spread from "./Spread"
import domtoimage from "dom-to-image"
import { touchesElement } from "../Util.ts"

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
    });
  }

  render() {
    return (
      <div className="iris-compound" ref={el => this.el = el}>
        {this.props.model.elements.map((model,i) => {
          const View = model.view;
          return (
            <div key={i}
              ref={el => { if (el != null) this.childNodes.set(i, el.childNodes[0]) }}>
              <View model={model} onDragStart={() => this.startDragging(model, i)} />
            </div>
        )})}
      </div>
    )
  }
}

export default class Manager {
  constructor() {
    this.view = View;
    this.name = "COMPOUND";
    this.layout = {
      x: 0, y: 0,
      w: 5, h: 3,
      minW: 2, maxW: 10,
      minH: 1, maxH: 10
    };
    this.elements = [
      new Spread(),
      new Spread()
    ]
  }
}
