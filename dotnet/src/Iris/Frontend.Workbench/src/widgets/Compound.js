import React, { Component } from 'react'
import Spread from "./Spread"
import domtoimage from "dom-to-image"
import { touchesElement } from "../Util.ts"

function startDragging(node, model, parentId, global) {
  if (node == null) {
    return;
  }

  domtoimage.toPng(node)
    .then(function (dataUrl) {
      console.log("drag start")
      const img = $("#iris-drag-image").attr("src", dataUrl).css({display: "block"});
      $(document)
        .on("mousemove.drag", e => {
          $(img).css({left:e.pageX, top:e.pageY});
          global.triggerEvent("drag", {
            type: "move",
            model: model,
            origin: parentId,
            x: e.clientX,
            y: e.clientY
          });
        })
        .on("mouseup.drag", e => {
          console.log("drag stop")
          img.css({display: "none"});
          global.triggerEvent("drag", {
            type: "stop",
            model: model,
            origin: parentId,
            x: e.clientX,
            y: e.clientY
          });
          $(document).off("mousemove.drag mouseup.drag");
        })
    })
    .catch(function (error) {
        console.error('Error when generating image:', error);
    });
}

class View extends Component {
  constructor(props) {
    super(props);
    this.childNodes = new Map();
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
              console.log("Add model");
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
              <View model={model} onDragStart={() => startDragging(this.childNodes.get(i), model, this.props.id, this.props.global)} />
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
