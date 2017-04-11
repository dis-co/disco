import React, { Component } from 'react'

export default class createProject extends Component {
  constructor(props) {
      super(props);
      this.state = {
        name: "",
        nameError: "Required",
        ipAddress: "",
        ipAddressError: "Required",
        apiPort: "",
        apiPortError: "Required",
        raftPort: "",
        raftPortError: "Required",
        webSocketPort: "",
        webSocketPortError: "Required",
        gitPort: "",
        gitPortError: "Required"
      };
  }

  isErrorName(id, name) {
    return typeof name === "string" && name.length > 0 ? null : "Required";
  }

  isErrorAddress(id, address) {
     return /^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$/.test(address) ? null : "Not a valid IP address";
  }

  isErrorPort(id, port) {
    // TODO: Check also range?
    if (isNaN(parseInt(port))) {
      return "Not a valid number";
    }
    for (let key in this.state) {
      if (key !== id && key.indexOf("Port") > -1 && !key.endsWith("Error")) {
        if (this.state[key] === port) {
          return "Duplicated port"
        }
      }
    }
    return null;
  }

  renderGroup(id, label, isError, placeholder = "") {
    const success = this.state[id + "Error"] == null;
    return (
      <div className="field">
        <label className="label">{label}</label>
        <p className="control has-icon has-icon-right">
          <input className={"input " + (success ? "is-success" : "is-danger")}
            type="text" placeholder={placeholder} value={this.state[id]} onChange={ev => {
              const value = ev.target.value;
              const error = isError(id, value);
              this.setState({ [id]: value, [id + "Error"]: error})
            }}/>
          <span className="icon is-small">
            <i className={"fa " + (success ? "fa-check" : "fa-warning")}></i>
          </span>
        </p>
        {!success && <p className="help is-danger">{this.state[id + "Error"]}</p>}
      </div>
    );
  }

  render() {
    const props = this.props;
    let isValid = true;
    for (let key in this.state) {
      if (key.endsWith("Error") && this.state[key] != null) {
        isValid = false;
        break;
      }
    }
    return (
      <div>
        <p className="title has-text-centered">Create Project</p>
        {this.renderGroup("name", "Name", this.isErrorName.bind(this))}
        {this.renderGroup("ipAddress", "IP Address", this.isErrorAddress.bind(this))}
        {this.renderGroup("apiPort", "Api Port", this.isErrorPort.bind(this))}
        {this.renderGroup("raftPort", "Raft Port", this.isErrorPort.bind(this))}
        {this.renderGroup("webSocketPort", "Web Socket Port", this.isErrorPort.bind(this))}
        {this.renderGroup("gitPort", "Git Daemon Port", this.isErrorPort.bind(this))}
        <div className="field is-grouped">
          <p className="control">
            <button className="button is-primary" disabled={!isValid} onClick={ev => {
              ev.preventDefault();
              Iris.createProject(this.state);
              props.onSubmit();
            }}>
              Submit
            </button>
          </p>
        </div>
      </div>
    );
  }
}