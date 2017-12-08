import React, { Component } from 'react'
import Switch, { Case, Default } from 'react-switch-case';

// This is a simple example to show how to create a custom widget for Disco
// in JS. We just define a simple React component that draws a square with
// black or transparent background depending on the value of a pin.

// Note the code uses several helpers, like `findPinByName`. These are
// available to JS in the `DiscoLib` global variable. The available methods
// can be seen in the Main.fs file of the Frontend.fsproj project. Other
// helpers can also be requested.

class Wat extends React.Component  {
  constructor(props){
    super(props)
    this.state={
      ip: false,
      pinpin : props.pinpin,
      setPinVal : props.setPinVal,
      isValid : true
    }
  }

  ipValid  (ev) {
    if(regValid(ev.target.value)){
      this.setState({ isValid: true })
      this.state.setPinVal(ev.target.value);
    } else {
      this.setState({ isValid: false })
    }
  }

  getFiles(ev) {
    var fileName = ev.target.files[0].name
    this.state.setPinVal(fileName)
  }

  getString(ev) {
    this.state.setPinVal(ev.target.value)
  }

  render(){
    var style = this.state.isValid ? {} : { border: "3px solid red" }
    if(this.state.pinpin !== null){
      switch(this.state.pinpin){
        case 'Simple':
          return <input type="text" onChange={this.getString} />
        break;
        case 'MultiLine':
          return <textarea onChange={this.getString} />
        break;
        case 'IP':
          return <input style={style} type="text" onChange={this.ipValid.bind(this)} />
        case 'FileName':
          return < input type='file' id='input' onChange={this.getFiles.bind(this)} />
        default:
          return <h1> nene </h1>
          break;

      }
    }
  }

}

//validates ip addresses
const regValid = function (str){
  var regex = /^([0-9]{1,3})\.([0-9]{1,3})\.([0-9]{1,3})\.([0-9]{1,3})$/
  try {
    var [m, b1, b2, b3, b4] = str.match(regex)
    if(parseInt(b1,10) > 255) return false
    if(parseInt(b2,10) > 255) return false
    if(parseInt(b3,10) > 255) return false
    if(parseInt(b4,10) > 255) return false
  } catch(e) {
    return false
  }
  return true
}

class TestWidget extends React.Component {
  constructor(props) {
    super(props);
    this.state={
      groupName: "",
      pinName: "",
      groupPin: "",
      pinVal: "",
      pin: null
    };
  }

  setPinVal(value) {
    this.state.pinVal = value;
    var pin = DiscoLib.findPinByName(this.props.model, this.state.groupPin);
    DiscoLib.updatePinValueAt(pin, 0, value)
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
    var pin = DiscoLib.findPinByName(this.props.model, groupPin);
    this.setState({
      groupPin: groupPin,
      pin: pin,
      pinVal: pin ? DiscoLib.getPinValueAt(pin, 0) : ""
    })
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
        <label>value</label>
          {
            this.state.pin !== null ?
            <Wat pinpin={this.state.pin.data.Behavior.ToString()}  pinVal={this.state.pinVal} setPinVal={this.setPinVal.bind(this)} />
            :
            <h1>pin is null</h1>
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
      minW: 2,  minH: 2,
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
      return DiscoLib.renderWidget(id, name, null, body, dispatch, model);
    }
  }
}
