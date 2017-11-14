import React, { Component } from 'react'
//import { Switch, Case } from "switch-case"
import Switch, { Case, Default } from 'react-switch-case';

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
      pinName: "",
      groupPin: "",
      pinVal: "",
      pin: null,
      picUrl: ""
    };
  }

  setPinVal(value) {
    console.log("setPinVal", value)
    this.state.pinVal = value;
    var pin = IrisLib.findPinByName(this.props.model, this.state.groupPin);
    IrisLib.updatePinValueAt(pin, 0, value)
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
    let groupPin = this.state.groupName + '/'+ this.state.pinName
    //set pin to this states current pin by pinName
    var pin = IrisLib.findPinByName(this.props.model, groupPin);
    this.setState({ 
      groupPin: groupPin,
      pin: pin,
      pinVal: pin ? IrisLib.getPinValueAt(pin, 0) : ""
    }, () => {
      console.log('pin has been changed: ', this.state.groupPin)
      console.log(this.state.pinVal)
      console.log("pin " + pin)
      if(pin !== null)
        console.log('hallo i bims 1 pin: '+ pin)
        this.convertBytes()
        console.log('url: ' + this.state.picUrl)
    })
  }

  //converts Uint8Array to blob and creates an url 
  convertBytes() {
    var blob = new Blob( [this.state.pinVal]);
    this.setState({
      picUrl : URL.createObjectURL(blob)
    })

  }

  cleanUp() {
    URL.revokeObjectURL(this.state.picUrl)
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
          pin name
          {/*onChange updates state with new pinName as read from input field*/}
          <input type="text" onChange={this.makeCallback("pinName")} />
        </label>
          {/*after pressing submit button this.state.groupName is updated to hold the full pin name*/}
        <button type="submit" onClick={this.setPin.bind(this)}>submit</button>
      </div>
        <div style={{margin: "0 10px"}}>
        {/*onChhange updates the state with new slider maximum value*/}
        <label></label>
        {
        this.state.pin?
        <img src={this.state.picUrl} style={{width: 100, height: 100}} onLoad={this.cleanUp.bind(this)} />
        : 
        <h6>nothing</h6>          
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
        return <TestWidget groupName="foo" pinName="VVVV/design.4vp/Z" pinVal="ho"  model={model} />
      }
      return IrisLib.renderWidget(id, name, null, body, dispatch, model);
    }
  }
}
