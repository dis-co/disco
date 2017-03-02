import React, { Component } from 'react'
import Spread from "./Spread"
import Draggable from "react-draggable"

class View extends Component {
  constructor(props) {
    super(props);
    this.state = {};
  }

  render() {
    return (
      <div className="iris-compound">
        {this.props.model.elements.map((el,i) => {
          // TODO: Check if the element is fixed
          const View = el.view;
          let draggableProps = {}, cssPosition = "absolute";
          if (!this.state.dragging) {
            draggableProps = { position: null };
            cssPosition = "static";
          }
          return (
            <Draggable key={i}
              {...draggableProps}
              onStart={ev => {
                this.setState({dragging: true})
              }}
              onDrag={(e,pos) => {
                this.props.global.triggerEvent("drag", {
                  type: "move",
                  x: e.clientX,
                  y: e.clientY
                });
              }}
              onStop={e => {
                this.props.global.triggerEvent("drag", {
                  type: "stop",
                  x: e.clientX,
                  y: e.clientY
                });
                this.setState({dragging: false})
              }}>
              <div style={{backgroundColor: "red", position: cssPosition, height: 50, width: 100}} />
            </Draggable>
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
