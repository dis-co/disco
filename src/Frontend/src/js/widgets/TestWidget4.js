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

const Wat = (props) => {

  if(props.pinpin !== null){
    switch(props.pinpin){
      case 'Simple':
        return <input type="text"
        onChange={(event) => {
            changeVal(props.context, 'pinVal', () => {
                IrisLib.updatePinValueAt(pin, 0, props.pinVal)
            })
            }} />
           
      break;
      case 'MultiLine':
        return <textarea
        onChange={(event) => {
            changeVal(props.context, 'pinVal', () => {
                IrisLib.updatePinValueAt(pin, 0, props.pinVal)
            })
            }} />
      break;

      default:
            return <h1> nene </h1>
            break;
  
    }
  }
}

const changeVal = (context, prop) => {
    return (event) => {
      var state = {}
      state[prop] = event.target.value
      context.setState(state)
    }
} 

class TestWidget extends React.Component {
  constructor(props) {
    super(props);
    //initialize 
    this.state={
      groupName: "",
      pinName: "",
      groupPin: "",
      pinVal: "",
    };
    //this.changeVal = this.changeVal.bind(this);
  }

  render() {
    //initialize pinVal
    var pinVal = '';
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
        onChange={changeVal(this, "groupName")} />
        </label>
        {/*input to select a pins name*/}
        <label>
          pin name
          {/*onChange updates state with new pinName as read from input field*/}
        <input type="text"
        onChange={changeVal(this, "pinName")} />
        </label>
          {/*after pressing submit button this.state.groupName is updated to hold the full pin name*/}
        <button type="submit" onClick={() => {
          this.setState({groupPin: this.state.groupName + '/'+ this.state.pinName}, () => {
            console.log('pin has been changed: ', this.state.groupPin)
            console.log(pinVal)
            console.log("pin " + pin)
            if(pin !== null)
              console.log(pin.data.Behavior.ToString())
          })
          
          ;}}>submit</button>
      </div>
        <div style={{margin: "0 10px"}}>
        {/*onChhange updates the state with new slider maximum value*/}
        <label>
            value
            </label>
            {
              pin !== null ?
              <Wat pinpin={pin.data.Behavior.ToString()}  pinVal={this.state.pinVal} context={this} />
              :
              <h1>nene</h1>
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
