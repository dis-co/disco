import React, { Component } from 'react'

export default class ProjectConfig extends Component {
  constructor(props) {
    super(props);
    let filtered = props.data ? props.data.filter(nid => nid.Name === "default") : [];
    this.state = {
      selected: filtered[0],
      name: ""
    };
  }

  render() {
    return (
      <div>
        <p className="title has-text-centered">Project Config</p>
        <p>
          Select a cluster in the project configuration or type a name to create a new one.
          The current machine will be added to the selected cluster if not present.
        </p>
        <div className="field">
          <label className="label">Available Sites</label>
          <p className="control">
            <span className="select">
              <select onChange={ev => this.setState({ selected: this.props.data[ev.target.selectedIndex] })}>
                {this.props.data.map((site,i) =>
                    <option key={i} value={site.Id}>{site.Name}</option>
                )}
              </select>
            </span>
          </p>
        </div>
        <div className="field">
          <label className="label">Create Project Site</label>
          <p className="control">
            <input className="input" type="text" value={this.state.name} onChange={ev => this.setState({ name: ev.target.value, selected: DiscoLib.createNameAndId(ev.target.value) }) }/>
          </p>
        </div>
        <div className="field">
          <p className="control">
            <button className="button is-primary"  disabled={this.state.selected == null} onClick={ev => {
                ev.preventDefault();
                this.props.onSubmit(this.state.selected);
              }}>
              Submit
            </button>
          </p>
        </div>
      </div>
    );
  }
}
