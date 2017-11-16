import React, { Component } from 'react'

import Select from 'react-select';
// Be sure to include styles at some point, probably during your bootstrapping
import 'react-select/dist/react-select.css';

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
      inputVal: "",
      pin: null,
      value: "",
      options : []
      }
  }


  //check if property "inputVal" exists and set as new pinVal?
  setPinVal() {
    console.log("trying to set pinval");
    
    /*
    var mapp = this.state.pin.data.Properties.reduce(function(map, obj) {
      map[obj.Key] = obj.Value;
      return map;
    }, {});

    for(var key in this.state.pin.data.Properties){
      console.log("trying 2");
      if(mapp.hasOwnProperty(key)){
        console.log("trying 3");
        if(mapp[key] == this.state.inputValue){
          this.state.pinVal = key;
          console.log("trying4");
          console.log(key);
        }
      }
    }
    */
    //this.state.pinVal = value;
    //var pin = IrisLib.findPinByName(this.props.model, this.state.groupPin);
    if (this.state.pin && this.state.inputVal) {  
      IrisLib.updatePinValueAt(this.state.pin, 0, this.state.inputVal.Key)    
    }
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
    console.log("setPin test1");
    let options = 
      pin 
        ? pin.data.Properties.map(prop => { return { label: prop.Value, value: prop.Key } }) 
        : []

    this.setState({ 
      groupPin: groupPin,
      pin: pin,
      options: options,
      pinVal: pin ? IrisLib.getPinValueAt(pin, 0) : ""
    }, () => {
      console.log('pin has been changed: ', this.state.groupPin)
    })
  }

  changeEnum(){
    console.log("pin properties: " + this.state.pin.data.Properties);
    console.log("pin:  ", this.state.pin);
    console.log("pin value ->  " , this.state.pinVal);
    console.log("inPut: ", this.state.inputVal);
  
  }

  //hadnles button click
  click(event){
    this.setPin();
    if(this.state.pin){
      this.changeEnum();
      //set pinVal from here?
      //this.setPinVal();
    }
  }

  
    
  logChange(val) {
    console.log('Selected: ', val);
    this.setState({
      value : val,
      inputVal:{
        Key: val.value, Value: val.label
      }
    }, this.setPinVal);
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
        <label>
          value
          {/*onChange updates state with new pinName as read from input field*/}
          <input type="text" disabled={this.state.pin == null} onChange={(event) => {
            this.setState({
              inputVal: event.target.value
            }, this.setPinVal.bind(this));
          }} />
        </label>
          {/*after pressing submit button this.state.groupName is updated to hold the full pin name*/}
        <button type="submit" onClick={this.click.bind(this)}>submit</button>
      </div>
        <div style={{margin: "0 10px"}}>
        {/*onChhange updates the state with new slider maximum value*/}
        <label>select</label>
          <Select
          name="form-field-name"
          
          value={this.state.value}
          options={this.state.options}
          onChange={this.logChange.bind(this)}
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
        return <TestWidget groupName="foo" pinName="VVVV/design.4vp/Z" pinVal="ho"  model={model} />
      }
      return IrisLib.renderWidget(id, name, null, body, dispatch, model);
    }
  }
}
