import * as React from "react";

export default class Form extends React.Component {
  constructor(props) {
    super(props);
    this.click = false;
  }

  render() {
    return (
      <form style={{margin: 10}}>
        <input type="text" name="value" value={this.props.globalState.value}
            onChange={ev => this.props.setGlobalState({value: ev.target.value})} />
        <input type="button" value="Add Row"
          onClick={() => {
            var rows = this.props.globalState.rows;
            this.props.setGlobalState({rows: rows.concat(rows.length + 1)})
          }} />
        <input type="button" value="Remove Row"
          onClick={() => {
            var rows = this.props.globalState.rows;
            if (rows.length > 0)
              this.props.setGlobalState({rows: rows.slice(0, rows.length - 2)})
          }} />
      </form>
    )
  }
}
