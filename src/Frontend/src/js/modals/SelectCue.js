import React, { Component } from 'react'

export default class SelectCue extends Component {
  constructor(props) {
      super(props);
      this.state = {};
  }

  render() {
    return (
      <div>
        <p className="title has-text-centered">Select Cue</p>
        <table className="table is-striped is-bordered"
          style={{width: "80%", margin: "0 auto"}}>
          <tbody>
            {this.props.data.map(nameAndId =>
              <tr key={nameAndId.Id.ToString()}>
                <td className="has-text-centered" style={{cursor: "pointer"}}
                  onClick={ev => {
                    this.props.onSubmit(nameAndId.Id);
                  }}>
                  {nameAndId.Name}
                </td>
              </tr>)}
          </tbody>
        </table>
      </div>
    );
  }
}
