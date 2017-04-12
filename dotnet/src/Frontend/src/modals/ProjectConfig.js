import React, { Component } from 'react'

export default class ProjectConfig extends Component {
  constructor(props) {
    super(props);
    this.state = {};
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
          <label className="label">Sites</label>
          <p className="control">
            <span className="select">
              <select>
                {this.props.sites.map((site,i) =>
                  <option key={i} onClick={ev => this.setState({selected: site})}>{site}</option>)}
              </select>
            </span>
          </p>
        </div>
          {/*<Input style={{
            display: "inline-block",
            marginLeft: 20
          }} label="Site" floatingLabel={true} required={false} value={this.state.selected} onChange={ev => this.setState({selected: ev.target.value})} />*/}
        <div className="field">
          <label className="label">Site</label>
          <p className="control">
            <input className="input" type="text" value={this.state.selected} onChange={ev => this.setState({selected: ev.target.value})}/>
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
