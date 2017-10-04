import React, { Component } from 'react'
import Slider from 'react-rangeslider';
import './TestWidget1.css';
// To include the default styles
import 'react-rangeslider/lib/index.css';



// This is a simple example to show how to create a custom widget for Iris
// in JS. We just define a simple React component that draws a square with
// black or transparent background depending on the value of a pin.

// Note the code uses several helpers, like `findPinByName`. These are
// available to JS in the `IrisLib` global variable. The available methods
// can be seen in the Main.fs file of the Frontend.fsproj project. Other
// helpers can also be requested.

//var Slider = require('react-rangeslider');

class TestWidget extends React.Component {
  constructor(props) {
    super(props);
    this.state = {
      pinVal:0
    }
  
  }

  /*handles slider change
  should update pin value with value 
  and set pinVal to current pinValue*/
  handleChange = (value) => {
    this.setState(
      {pinVal : IrisLib.updatePinValueAt(pin, 0, value)}
    );
  };
  

  render() {

    //not sure about this
    let {pinVal} = this.state;

    var active = false;
    var pin = IrisLib.findPinByName(this.props.model, this.props.pinName);
    if (pin != null) {
      var pinValue = IrisLib.getPinValueAt(pin, 0);
      active = typeof pinValue === "number" && pinValue > 10;
    }
    return (
      <div style={{
        display: "flex",
        flexDirection: "column",
        justifyContent: "center",
        height: "100%"
      }}>
        <div style={{
          width: "30px",
          height: "30px",
          margin: "0px auto",
          border: "2px solid black",
          backgroundColor: active ? "black" : "inherit"
        }} />
        <div className="slider">
          <Slider 
            value={pinVal}
            orientation="horizontal"
            onChange={this.handleChange}
            min={parseInt("-100",10)}
            max={parseInt("100", 10)}
            />
            </div>
      
      </div>
    )
  }
}

// The widget scripts must export a function that receives an id
// and returns an object with the following properties, this may
// change a little bit to make the API more usable from JS.
export default function createWidget (id, name) {
  return {
    Id: id ,
    Name: name,
    InitialLayout: {
      i: id, static: false,
      x: 0, y: 0, w: 3, h: 3,
      minW: 2, maxW: 6, minH: 2, maxH: 6
    },
    // The Render method receives a dispatch function to send messages
    // to the global state and a model represing the current snapshot of
    // the state. The Render method must return a React element.
    Render(dispatch, model) {
      // Here we use the `renderWidget` which accepts a function to
      // render the body and optionally another to render a header in
      // the title bar of the widget.
      var body = function (dispatch, model) {
        return <TestWidget pinName="VVVV/design.4vp/Z" model={model} />
      }
      return IrisLib.renderWidget(id, name, null, body, dispatch, model);
    }
  }
}
