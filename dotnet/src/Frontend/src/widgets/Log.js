import React, { Component } from 'react'
import {Table, Column, Cell} from 'fixed-data-table'
import "fixed-data-table/dist/fixed-data-table.css"
import { map } from "../Util.ts"

class View extends Component {
  constructor(props) {
    super(props);
    var logs = props.global.state.logs;
    this.state = { filter: "", logs: logs, filteredLogs: logs };
    IrisLib.subscribe(props.model.filterChange, filter => {
      this.updateState(this.state.logs, filter)
    });
  }

  updateState(logs, filter) {
    var filteredLogs = logs;
    if (filter) {
      var reg = new RegExp(filter, "i");
      filteredLogs = logs.filter(log => reg.test(log.Message));
    }
    this.setState({filter: filter, logs: logs, filteredLogs: filteredLogs})
  }

  componentDidMount() {
    this.disposable =
      this.props.global.subscribe("logs", logs =>
        this.updateState(logs, this.state.filter));
  }

  componentWillUnmount() {
    if (this.disposable) {
      this.disposable.dispose();
    }
  }

  render() {
   return (
    <Table
      rowsCount={this.state.filteredLogs.length}
      rowHeight={30}
      headerHeight={30}
      width={800}
      height={600}
      >
        <Column width={100} header={<Cell>LogLevel</Cell>} cell={({rowIndex, ...props}) => (
            <Cell {...props}>{IrisLib.toString(this.state.filteredLogs[rowIndex].LogLevel)}</Cell>
        )} />
        <Column width={100} header={<Cell>Time</Cell>} cell={({rowIndex, ...props}) => (
            <Cell {...props}>{new Date(this.state.filteredLogs[rowIndex].Time).toLocaleTimeString()}</Cell>
        )} />
        <Column width={100} header={<Cell>Tag</Cell>} cell={({rowIndex, ...props}) => (
            <Cell {...props}>{this.state.filteredLogs[rowIndex].Tag}</Cell>
        )} />
        <Column width={100} header={<Cell>Tier</Cell>} cell={({rowIndex, ...props}) => (
            <Cell {...props}>{IrisLib.toString(this.state.filteredLogs[rowIndex].Tier)}</Cell>
        )} />
        <Column width={400} header={<Cell>Message</Cell>} cell={({rowIndex, ...props}) => (
            <Cell {...props} style={{whiteSpace: "nowrap"}}>
              {this.state.filteredLogs[rowIndex].Message}
            </Cell>
        )} />
      </Table>
    )
  }
}

export default class Log {
  constructor() {
    this.view = View;
    this.name = "LOG";
    this.filterChange = IrisLib.createObservable();
    this.titleBar =
      <input type="text" placeholder="Filter by regex..." ref={input => {
        input.addEventListener("change", _ => {
          this.filterChange.trigger(input.value)
        })
      }} />
    this.layout = {
      x: 0, y: 0,
      w: 8, h: 6,
      minW: 2, maxW: 20,
      minH: 2, maxH: 20
    }
  }
}