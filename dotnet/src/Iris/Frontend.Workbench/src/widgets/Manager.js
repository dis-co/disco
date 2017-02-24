import React, { Component } from 'react'
import css from "../../css/Manager.less"

export default class Manager extends Component {
  static get layout() {
    return {
      x: 5, y: 0,
      w: 5, h: 3,
      minW: 2, maxW: 10,
      minH: 2, maxH: 10
    };
  }

  constructor(props) {
    super(props);
    this.state = {};
  }

  render() {
    var model = this.props.model;
    return (
      <div className="iris-manager">
        <div className="iris-draggable-handle">
          <span>MANAGER</span>
          <span className="iris-close" onClick={() => {
            debugger;
            this.props.model.removeWidget(this.props.id);
          }}>x</span>
        </div>
        <form>
          <div>
            <input className="iris-fill" type="text" value={this.state.log} onChange={ev => this.setState({log: ev.target.value})} />
            <input className="iris-button" type="button" value="Add Log" onClick={() => model.addLog(this.state.log)} />
            <input className="iris-button" type="button" value="Remove Log" onClick={() => model.removeLastLog()} />
          </div>
          {/*<input type="text" name="value" value={this.props.globalState.value}
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
      </div>
    )
  }
}
