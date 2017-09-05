/// @ts-check

import React, { Component } from 'react'

export default class LoadProject extends Component {
  constructor(props) {
    super(props);
  }

  render() {
    return (
      <div>
        <p className="title has-text-centered">Available Projects</p>
        <table className="table is-striped is-bordered"
          style={{width: "50%", margin: "0 auto"}}>
          <tbody>
            {this.props.data.map(name =>
              <tr key={name}>
                <td className="has-text-centered"
                  onClick={ev => {
                    ev.preventDefault();
                    this.props.onSubmit(name);
                  }}>
                  {name}
                </td>
              </tr>)}
          </tbody>
        </table>
        <div className="field is-grouped">
          <p className="control">
            <button className="button is-primary" onClick={ev => {
              ev.preventDefault();
              this.props.onSubmit();
            }}>
              Create new project
            </button>
          </p>
        </div>
      </div>
    );
  }
}