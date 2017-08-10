import React, { Component } from 'react'

class TestWidget extends Component {
  constructor(props) {
    super(props);
    this.state = { html: "" };
  }

  fetchHTML() {
    fetch('/css/test.html')
      .then(res => res.text())
      .then(html => this.setState({html}));
  }

  componentDidMount() {
    this.fetchHTML();
    this.intervalID = window.setInterval(this.fetchHTML.bind(this), 2000);
  }

  componentWillUnmount() {
    window.clearInterval(this.intervalID);
  }  

  render() {
    return (
      <div
        className="iris-test-widget"
        dangerouslySetInnerHTML={{__html: this.state.html}}>
      </div>
    )
  }
}

export default class TestWidgetModel {
  constructor() {
    this.view = TestWidget;
    this.name = "Test Widget";
    this.layout = {
      x: 0, y: 0,
      w: 3, h: 6,
      minW: 1, maxW: 10,
      minH: 1, maxH: 10
    }
  }
}