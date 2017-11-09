import React, { Component } from 'react'

import InputSlider from 'react-input-slider'
import 'react-input-slider/dist/input-slider.css';

// This is a simple example to show how to create a custom widget for Iris
// in JS. We just define a simple React component that draws a square with
// black or transparent background depending on the value of a pin.

// Note the code uses several helpers, like `findPinByName`. These are
// available to JS in the `IrisLib` global variable. The available methods
// can be seen in the Main.fs file of the Frontend.fsproj project. Other
// helpers can also be requested.


class TestWidget extends React.Component {
  constructor(props) {
    super(props);
    //initialize 
    this.state={
      groupName: "",
      pinNameX: "",
      pinNameY: "",
      groupPinX: "",
      groupPinY: "",
      pinValX: "",
      pinValY: "",
      pinX: null,
      pinY: null
    };
  }

  setPinValX(value) {
    console.log("setPinValX", value)
    this.state.pinValX = value;
    var pinX = IrisLib.findPinByName(this.props.model, this.state.groupPinX);
    IrisLib.updatePinValueAt(pinX, 0, value)
  }
  setPinValY(value) {
    console.log("setPinValY", value)
    this.state.pinValY = value;
    var pinY = IrisLib.findPinByName(this.props.model, this.state.groupPinY);
    IrisLib.updatePinValueAt(pinY, 0, value)
  }

  //event handler for onChange methods, to set parents state
  //from child component
  //param: context (this), prop (property as string)
  makeCallback(propName) {
    return (ev) => {
      var state = {}
      state[propName] = ev.target.value
      this.setState(state)
    }
  }

  setPin() {
    let groupPinX = this.state.groupName + '/'+ this.state.pinNameX
    let groupPinY = this.state.groupName + '/'+ this.state.pinNameY
    
    //set pin to this states current pin by pinName
    var pinX = IrisLib.findPinByName(this.props.model, groupPinX);
    var pinY = IrisLib.findPinByName(this.props.model, groupPinY);
    
    this.setState({ 
      groupPinX: groupPinX,
      groupPinY: groupPinY,
      pinX: pinX,
      pinY: pinY,
      pinValX: pinX ? IrisLib.getPinValueAt(pinX, 0) : "",
      pinValY: pinY ? IrisLib.getPinValueAt(pinY, 0) : ""
    }, () => {
      console.log('pinX has been changed: ', this.state.groupPinX)
      console.log(this.state.pinValX)
      console.log("pinX " + pinX)
    

     console.log('pinY has been changed: ', this.state.groupPinY)
     console.log(this.state.pinValY)
     console.log("pinY " + pinY)
    
    })
  }

  sliderChange = pos => {
   /* this.setState({
        pinValX: pos.x,
        pinValY: pos.y
    })
    */
    this.setPinValX(pos.x)
    this.setPinValY(pos.y)
}

  render() {
    return (
      <div style={{
        display: "flex",
        flexDirection: "column",
        justifyContent: "center",
        height: "100%"
      }}>
      <div>
        {/*input to select a pins group*/}
        <label>
          group name
          {/*onChange updates state with new groupName as read from input field*/}
          <input type="text" onChange={this.makeCallback("groupName")} />
        </label>
        {/*input to select a pins name*/}
        <label>
          x pin
          {/*onChange updates state with new pinName as read from input field*/}
          <input type="text" onChange={this.makeCallback("pinNameX")} />
        </label>
        <label>
          y pin
          {/*onChange updates state with new pinName as read from input field*/}
          <input type="text" onChange={this.makeCallback("pinNameY")} />
        </label>
        <label>
          x value
          {/*onChange updates state with new pinName as read from input field*/}
          <input type="text" onChange={(event) => this.setPinValX(event.target.value)} />
        </label>
        <label>
          y value
          {/*onChange updates state with new pinName as read from input field*/}
          <input type="text" onChange={(event) => this.setPinValY(event.target.value)} />
        </label>
          {/*after pressing submit button this.state.groupName is updated to hold the full pin name*/}
        <button type="submit" onClick={this.setPin.bind(this)}>submit</button>
      </div>
        <div style={{margin: "0 10px"}}>
        {/*onChhange updates the state with new slider maximum value*/}

        {(this.state.pinX && this.state.pinY) !== null ?
          <InputSlider
          style={{width:"112px", height:"112px", background:"grey"}}
          className='slider slider-xy'
          axis='xy'
          
          x={parseInt(this.state.pinValX,10)}
          xmax={100}
          y={parseInt(this.state.pinValY,10)}
          ymax={100}
          onChange={this.sliderChange}
          />
          : <h6>huhu</h6>
        }
          
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
        return <TestWidget groupName="foo" pinNameX="VVVV/design.4vp/Z" pinNameY="VVVV/design.4vp/Z" pinVal="ho"  model={model} />
      }
      return IrisLib.renderWidget(id, name, null, body, dispatch, model);
    }
  }
}
