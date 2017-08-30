

import React, { Component } from 'react'

function isActivated(model) {
  var pin = IrisLib.findPinByName(model, "VVVV/design.4vp/Z");
  if (pin != null) {
    var value = IrisLib.getPinValueAt(pin, 0);
    return value > 10;
  }
  return false;
}

function render (dispatch, model) {
  var active = isActivated(model);
  return <div style={{
    width: "20px", height: "20px",
    border: "2px solid black",
    backgroundColor: active ? "black" : "inherit"
  }} />;
}

export default function createWidget (id) {
  return {
    Id: id ,
    Name: "Test" ,
    InitialLayout: {
      i: id, static: false,
      x: 0, y: 0, w: 2, h: 2,
      minW: 2, maxW: 4, minH: 2, maxH: 4
    },
    Render(dispatch, model) {
      return IrisLib.renderHeadlessWidget(id, "Test", render, dispatch, model);
    }
  }
}
