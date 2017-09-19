import React, { Component } from 'react'

export default class LoadProject extends Component {
  constructor(props) {
    super(props);
    this.state = {};
  }

  render() {
    return (
      <div>
        <p className="title has-text-centered">Available Projects</p>
        <table className="table is-striped is-bordered"
          style={{width: "80%", margin: "0 auto"}}>
          <tbody>
            {this.props.data.map(name =>
              <tr key={name}>
                <td className="has-text-centered" style={{cursor: "pointer"}}
                  onClick={ev => {
                    this.props.onSubmit(name);
                  }}>
                  {name}
                </td>
              </tr>)}
          </tbody>
        </table>
      </div>
    );
  }
}
