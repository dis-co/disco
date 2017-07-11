import React, { Component } from 'react'
import {Table, Column, Cell} from 'fixed-data-table'
import "fixed-data-table/dist/fixed-data-table.css"
import { map } from "../Util.ts"

const SortTypes = {
  ASC: 1,
  DESC: -1
};

class Sort {
  /**
   * @param {string} column
   * @param {number} direction
   */
  constructor(column, direction) {
    this.column = column;
    this.direction = direction;
  }

  equals(sort) {
    return sort instanceof Sort
      ? this.column === sort.column && this.direction === sort.direction
      : false;
  }
}

const SortableCell = props => {
  debugger;
  const col = typeof props.children === "string" ? props.children : "";
  return (
    <Cell style={{cursor: "pointer"}}
      onClick={ev => props.onClick(col)}
    >{col + (props.sort && col === props.sort.column ?
      (props.sort.direction === SortTypes.ASC ? '↓' : '↑') : "")}</Cell>
  );
}

class View extends Component {
  constructor(props) {
    super(props);
    var logs = props.global.state.logs;
    this.state = { filter: null, sort: null, logs: logs, viewLogs: logs };
    IrisLib.subscribe(props.model.filterChange, filter => {
      this.updateState({filter})
    });
  }

  updateState({
    logs = this.state.logs,
    filter = this.state.filter,
    sort = this.state.sort
  }) {
    let viewLogs = logs;
    if (filter) {
      let reg = new RegExp(filter, "i");
      viewLogs = viewLogs.filter(log => reg.test(log.Message));
    }
    if (sort) {
      viewLogs = viewLogs.sort((log1,log2) => {
        let col1 = IrisLib.toString(log1[sort.column]), col2 = IrisLib.toString(log2[sort.column]);
        var res = col1 < col2 ? 1 : (col1 === col2 ? 0 : -1);
        return sort.direction === SortTypes.ASC ? res : res * -1;
      })
    }
    this.setState({filter, sort, logs, viewLogs})
  }

  componentDidMount() {
    this.disposable =
      this.props.global.subscribe("logs", logs =>
        this.updateState({logs}));
  }

  componentWillUnmount() {
    if (this.disposable) {
      this.disposable.dispose();
    }
  }

  onSorted(col) {
    debugger;
    let sort = this.state.sort;
    if (sort instanceof Sort && sort.column === col) {
      sort = new Sort(col, sort.direction * -1);
    }
    else {
      sort = new Sort(col, SortTypes.ASC);
    }
    this.updateState({sort});
  }

  render() {
   const onSorted = this.onSorted.bind(this);
   return (
    <Table
      rowsCount={this.state.viewLogs.length}
      rowHeight={30}
      headerHeight={30}
      width={800}
      height={600}
      >
        <Column width={100}
          header={<SortableCell sort={this.state.sort} onClick={onSorted}>LogLevel</SortableCell>}
          cell={({rowIndex, ...props}) => (
            <Cell {...props}>{IrisLib.toString(this.state.viewLogs[rowIndex].LogLevel)}</Cell>
        )} />
        <Column width={100}
          header={<SortableCell sort={this.state.sort} onClick={onSorted}>Time</SortableCell>}
          cell={({rowIndex, ...props}) => (
            <Cell {...props}>{new Date(this.state.viewLogs[rowIndex].Time).toLocaleTimeString()}</Cell>
        )} />
        <Column width={100}
          header={<SortableCell sort={this.state.sort} onClick={onSorted}>Tag</SortableCell>}
          cell={({rowIndex, ...props}) => (
            <Cell {...props}>{this.state.viewLogs[rowIndex].Tag}</Cell>
        )} />
        <Column width={100}
          header={<SortableCell sort={this.state.sort} onClick={onSorted}>Tier</SortableCell>}
          cell={({rowIndex, ...props}) => (
            <Cell {...props}>{IrisLib.toString(this.state.viewLogs[rowIndex].Tier)}</Cell>
        )} />
        <Column width={400}
          header={<Cell>Message</Cell>}
          cell={({rowIndex, ...props}) => (
            <Cell {...props} style={{whiteSpace: "nowrap"}}>
              {this.state.viewLogs[rowIndex].Message}
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