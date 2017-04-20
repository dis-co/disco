import React from 'react';
import Form from 'muicss/lib/react/form';
import Input from 'muicss/lib/react/input';
import Button from 'muicss/lib/react/button';
import { addMember } from "iris";

export default class AddNode extends Component {
  constructor(props) {
      super(props);
      let match = /\:(\d+)$/.exec(window.location.host);
      this.webPort = match != null ? match[1] : null;
      this.state = {
        id: "",
        idError: "Required",
        hostName: "",
        hostNameError: "Required",
        ipAddr: "",
        ipAddrError: "Required",
        port: "",
        portError: "Required",
        apiPort: "",
        apiPortError: "Required",
        wsPort: "",
        wsPortError: "Required",
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
    if (this.webPort && port === this.webPort) {
      return "Port in use by web server"
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
    let isValid = true;
    for (let key in this.state) {
      if (key.endsWith("Error") && this.state[key] != null) {
        isValid = false;
        break;
      }
    }    
    return (
      <div>
        <p className="title has-text-centered">Node information</p>
        {this.renderGroup("id", "Id", this.isErrorName.bind(this))}
        {this.renderGroup("hostName", "Host", this.isErrorName.bind(this))}
        {this.renderGroup("ipAddr", "IP Address", this.isErrorAddress.bind(this))}
        {this.renderGroup("port", "Port", this.isErrorPort.bind(this))}
        {this.renderGroup("apiPort", "Api Port", this.isErrorPort.bind(this))}
        {this.renderGroup("wsPort", "Web Socket Port", this.isErrorPort.bind(this))}
        {this.renderGroup("gitPort", "Git Daemon Port", this.isErrorPort.bind(this))}
        <div className="field is-grouped">
          <p className="control">
            <button className="button is-primary" disabled={!isValid} onClick={ev => {
              ev.preventDefault();
              Iris.addMember(this.state);              
              this.props.onSubmit();
            }}>
              Submit
            </button>
          </p>
        </div>
      </div>
    );
  }
}
