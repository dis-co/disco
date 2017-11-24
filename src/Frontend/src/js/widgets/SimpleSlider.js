import React, { Component } from 'react'
import Slider from 'react-rangeslider';
import './TestWidget1.css';
import 'react-rangeslider/lib/index.css';


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
      sliderMin: -100,
      sliderMax: 100,
      groupPin: ""
    };
  }

  render() {
    //initialize pinVal
    var pinVal = 0;
    //set pin to this states current pin by pinName
    var pin = IrisLib.findPinByName(this.props.model, this.state.groupPin);
    if (pin != null) {
      pinVal = IrisLib.getPinValueAt(pin, 0);
    }
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
        <input type="text"
        onChange={(event) => this.setState({groupName : event.target.value})} />
        </label>
        {/*input to select a pins name*/}
        <label>
          pin name
          {/*onChange updates state with new pinName as read from input field*/}
        <input type="text"
        onChange={(event) => this.setState({pinName: event.target.value})} />
        </label>
        {/*input to set minimum value*/}
        <label>
          minimum value
          {/*onChange updates the state with new slider minimum value*/}
          <input type="text" 
          onChange={(event) => this.setState({sliderMin: event.target.value})} />
        </label>
        {/*input to set maximum value*/}
        <label>
          maximum value
          {/*onChhange updates the state with new slider maximum value*/}
          <input type="text"
          onChange={(event) => this.setState({sliderMax: event.target.value})}/>
          </label>
          {/*after pressing submit button this.state.groupName is updated to hold the full pin name*/}
        <button type="submit" onClick={() => {
          this.setState({groupPin: this.state.groupName + '/'+ this.state.pinName})
          ;}}>submit</button>
      </div>
        <div style={{margin: "0 10px"}}>
        {/*slider has an onChange function that updates the selected pins value to sliders 
        current value pinVal. gets its min and max values from state*/}
          <Slider
            value={parseInt(pinVal, 10)}
            onChange={(value) => {
              if(pin != null)
                IrisLib.updatePinValueAt(pin, 0, value)
            }}
            min={parseInt(this.state.sliderMin, 10)}
            max={parseInt(this.state.sliderMax, 10)}
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
        return <TestWidget groupName="foo" pinName="VVVV/design.4vp/Z"  model={model} />
      }
      return IrisLib.renderWidget(id, name, null, body, dispatch, model);
    }
  }
}
