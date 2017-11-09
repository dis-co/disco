import React, { Component } from 'react'

export default class SelectCue extends Component {
  constructor(props) {
      super(props);
      this.state = {
        selected: {}
      };
  }

  render() {
    return (
      <div>
        <p className="title has-text-centered">Select Cue</p>
        <table className="table is-striped is-bordered"
          style={{width: "100%"}}>
          <tbody>
            {this.props.data.map(cue =>
              <tr key={cue.Id.ToString()}>
                <td style={{cursor: "pointer"}}>
                  <input class="checkbox" type="checkbox" onClick={ev => {
                      if(ev.target.checked) {
                        let selected = this.state.selected
                        selected[cue.Id.ToString()] = cue.Id
                        this.setState({ selected: selected })
                      }
                      if(!ev.target.checked) {
                        let selected = this.state.selected
                        delete selected[cue.Id.ToString()]
                        this.setState({ selected: selected })
                      }
                    }}/>
                </td>
                <td className="has-text-centered">
                  {cue.Name}
                </td>
              </tr>)}
          </tbody>
        </table>
        <div className="field">
          <p className="control">
            <button
              className="button is-primary"
              disabled={Object.keys(this.state.selected).length == 0}
              onClick={ev => {
                ev.preventDefault();
                this.props.onSubmit(Object.values(this.state.selected));
              }}>
              Submit
            </button>
          </p>
        </div>
      </div>
    );
  }
}
