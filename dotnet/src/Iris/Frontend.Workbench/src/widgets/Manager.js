import React, { Component } from 'react'
import css from "../../css/Manager.less"
import { getRandomInt } from "../Util.ts"

class View extends Component {
  constructor(props) {
    super(props);
    this.state = {log: ""};
  }

  render() {
    var model = this.props.model;
    return (
      <form className="iris-manager">
        <div>
          <input className="iris-fill" type="text" value={this.state.log} onChange={ev => this.setState({log: ev.target.value})} />
          <input className="iris-button" type="button" value="Add Log" onClick={() => model.addLog(this.state.log)} />
          <input className="iris-button" type="button" value="Remove Log" onClick={() => model.removeLastLog()} />
        </div>
        {/*<div>{this.props.model.value}</div>
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
          }} />*/}
      </form>
    )
  }
}

export default class Manager {
  constructor() {
    this.view = View;
    this.name = "MANAGER";
    // this.value = getRandomInt(0, 10);
    this.layout = {
      x: 5, y: 0,
      w: 5, h: 3,
      minW: 2, maxW: 10,
      minH: 2, maxH: 10
    };
  }
}