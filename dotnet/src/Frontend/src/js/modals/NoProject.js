/// @ts-check

import React, { Component } from 'react'

export default class LoadProject extends Component {
  constructor(props) {
    super(props);
  }

  render() {
    return (
      <div>
        <p className="title has-text-centered">Recent Projects</p>
        <div className="field is-grouped">
          <p className="control">
            <button className="button is-primary" onClick={ev => {
              ev.preventDefault();
              console.log("Hello world!")
              {/* this.props.onSubmit(this.state); */}
            }}>
              Submit
            </button>
          </p>
        </div>
      </div>
    );
  }
}