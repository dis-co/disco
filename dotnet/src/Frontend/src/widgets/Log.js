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
    this.state = {
      filter: null,
      sort: null,
      columns: {
        LogLevel: true,
        Time: true,
        Tag: true,
        Tier: true
      },
      logs: logs,
      viewLogs: logs
    };
    IrisLib.subscribe(props.model.observable, kv => {
      if (kv.key === "filter") {
        this.updateViewLogs({filter: kv.value})
      }
      else {
        this.setState({columns: Object.assign(this.state.columns, {[kv.key]: kv.value})});
      }
    });
  }

  updateViewLogs({
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
        this.updateViewLogs({logs}));
  }

  componentWillUnmount() {
    if (this.disposable) {
      this.disposable.dispose();
    }
  }

  onSorted(col) {
    let sort = this.state.sort;
    if (sort instanceof Sort && sort.column === col) {
      sort = new Sort(col, sort.direction * -1);
    }
    else {
      sort = new Sort(col, SortTypes.ASC);
    }
    this.updateViewLogs({sort});
  }

  render() {
   const onSorted = this.onSorted.bind(this);
   return (
    <Table
      rowsCount={this.state.viewLogs.length}
      rowHeight={30}
      headerHeight={30}
      width={700}
      height={600}
      >
        {this.state.columns.LogLevel ?
          <Column width={70}
            header={<SortableCell sort={this.state.sort} onClick={onSorted}>LogLevel</SortableCell>}
            cell={({rowIndex, ...props}) => (
              <Cell {...props}>{IrisLib.toString(this.state.viewLogs[rowIndex].LogLevel)}</Cell>
          )} /> : null}
        {this.state.columns.Time ?
          <Column width={80}
            header={<SortableCell sort={this.state.sort} onClick={onSorted}>Time</SortableCell>}
            cell={({rowIndex, ...props}) => (
              <Cell {...props}>{new Date(this.state.viewLogs[rowIndex].Time).toLocaleTimeString()}</Cell>
          )} /> : null }
        {this.state.columns.Tag ?
          <Column width={75}
            header={<SortableCell sort={this.state.sort} onClick={onSorted}>Tag</SortableCell>}
            cell={({rowIndex, ...props}) => (
              <Cell {...props}>{this.state.viewLogs[rowIndex].Tag}</Cell>
          )} /> : null }
        {this.state.columns.Tier ?
          <Column width={75}
            header={<SortableCell sort={this.state.sort} onClick={onSorted}>Tier</SortableCell>}
            cell={({rowIndex, ...props}) => (
              <Cell {...props}>{IrisLib.toString(this.state.viewLogs[rowIndex].Tier)}</Cell>
          )} /> : null }
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

class TitleForm extends Component {
  constructor(props) {
    super(props);
    this.state = {
      filter: "",
      LogLevel: true,
      Time: true,
      Tag: true,
      Tier: true
    };
  }

  render() {
    return (
      <div>
        <input type="text" placeholder="Filter by regex..."
          value={this.state.filter} onChange={ev => {
            this.props.observable.trigger({key: "filter", value: ev.target.value});
            this.setState({filter: ev.target.value});
          }} />
       <label style={{marginLeft: "10px"}}>
          LogLevel
          <input type="checkbox" checked={this.state.LogLevel}
            style={{marginLeft: "5px"}} onChange={ev => {
              this.props.observable.trigger({key: "LogLevel", value: ev.target.checked});
              this.setState({LogLevel: ev.target.checked});
            }} />
        </label>
       <label style={{marginLeft: "10px"}}>
          Time
          <input type="checkbox" checked={this.state.Time}
            style={{marginLeft: "5px"}} onChange={ev => {
              this.props.observable.trigger({key: "Time", value: ev.target.checked});
              this.setState({Time: ev.target.checked});
            }} />
        </label>
       <label style={{marginLeft: "10px"}}>
          Tag
          <input type="checkbox" checked={this.state.Tag}
            style={{marginLeft: "5px"}} onChange={ev => {
              this.props.observable.trigger({key: "Tag", value: ev.target.checked});
              this.setState({Tag: ev.target.checked});
            }} />
        </label>
       <label style={{marginLeft: "10px"}}>
          Tier
          <input type="checkbox" checked={this.state.Tier}
            style={{marginLeft: "5px"}} onChange={ev => {
              this.props.observable.trigger({key: "Tier", value: ev.target.checked});
              this.setState({Tier: ev.target.checked});
            }} />
        </label>
      </div>
    );
  }
}

export default class Log {
  constructor() {
    this.view = View;
    this.name = "LOG";
    this.observable = IrisLib.createObservable();
    this.titleBar = <TitleForm observable={this.observable} />;
    this.layout = {
      x: 0, y: 0,
      w: 8, h: 6,
      minW: 6, maxW: 20,
      minH: 2, maxH: 20
    }
  }
}