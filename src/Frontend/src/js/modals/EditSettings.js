import React, { Component } from 'react'

export default class EditSettings extends Component {
  constructor(props) {
    super(props);
    this.state = {
      useRightClick: props.data.useRightClick
    };
  }

  render() {
    return (
      <div>
        <p className="title has-text-centered">Settings</p>
        <table className="table is-striped is-bordered"
          style={{width: "100%"}}>
          <tbody>
            <tr key="use-right-click">
              <td style={{cursor: "pointer"}}>
                <input
                  className="checkbox"
                  defaultChecked={this.state.useRightClick}
                  type="checkbox"
                  onClick={ev => {
                    this.setState({ useRightClick: ev.target.checked })
                  }}/>
              </td>
              <td className="has-text-centered">Use Right-Click</td>
            </tr>
          </tbody>
        </table>
        <div className="field">
          <p className="control">
            <button
              className="button is-primary"
              onClick={ev => {
                ev.preventDefault();
                this.props.onSubmit(this.state.useRightClick);
              }}>
              Save
            </button>
          </p>
        </div>
      </div>
    );
  }
}
