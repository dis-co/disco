import React, { Component } from 'react'

export default class Login extends Component {
  constructor(props) {
    super(props);
    this.state = {
      project: props.data,
      username: "",
      usernameError: "Required",
      password: "",
      passwordError: "Required"
    };
  }

  isError(id, name) {
    return typeof name === "string" && name.length > 0 ? null : "Required";
  }

  renderGroup(id, label, isError, placeholder = "") {
    const success = this.state[id + "Error"] == null;
    return (
      <div className="field">
        <label className="label">{label}</label>
        <p className="control has-icon has-icon-right">
          <input className={"input " + (success ? "is-success" : "is-danger")}
            type={id === "password" ? "password" : "text"} placeholder={placeholder} value={this.state[id]} onChange={ev => {
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
        <p className="title has-text-centered">Login - {this.state.project}</p>
        {this.renderGroup("username", "Username", this.isError)}
        {this.renderGroup("password", "Password", this.isError)}
        <div className="field is-grouped is-grouped-right">
          <p className="control">
            <button className="button is-primary" onClick={ev => {
                ev.preventDefault();
                this.props.onSubmit({
                  name: this.state.project,
                  username: this.state.username,
                  password: this.state.password
                });
              }}>
              Login
            </button>
          </p>
        </div>
      </div>
    );
  }
}
