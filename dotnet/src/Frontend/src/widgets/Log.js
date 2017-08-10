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
      logLevel: "none",
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
      if (kv.key === "logLevel") {
        this.updateViewLogs({logLevel: kv.value})
      }
      else if (kv.key.startsWith("columns/")) {
        let key = kv.key.replace("columns/","")
        this.setState({columns: Object.assign(this.state.columns, {[key]: kv.value})});
      }
    });
  }

  updateViewLogs({
    logs = this.state.logs,
    filter = this.state.filter,
    logLevel = this.state.logLevel,
    sort = this.state.sort
  }) {
    let viewLogs = logs;
    if (filter) {
      let reg = new RegExp(filter, "i");
      viewLogs = viewLogs.filter(log => reg.test(log.Message));
    }
    if (logLevel && logLevel !== "none") {
      viewLogs = viewLogs.filter(log => IrisLib.toString(log.LogLevel) === logLevel);
    }
    if (sort) {
      viewLogs = viewLogs.sort((log1,log2) => {
        let col1 = IrisLib.toString(log1[sort.column]), col2 = IrisLib.toString(log2[sort.column]);
        var res = col1 < col2 ? 1 : (col1 === col2 ? 0 : -1);
        return sort.direction === SortTypes.ASC ? res : res * -1;
      })
    }
    this.setState({filter, sort, logLevel, logs, viewLogs})
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
      logLevel: "none",
      setLogLevel: "debug",
      columns: {
        LogLevel: true,
        Time: true,
        Tag: true,
        Tier: true
      }
    };
  }

  renderDropdown(title, values, generator) {
    return (
    <div className="iris-dropdown">
    <div className="iris-dropdown-button">{title}</div>
    <div className="iris-dropdown-content">
      {values.map(x => <div key={x}>{generator(x)}</div>)}
    </div>
  </div>
  )}

  render() {
    return (
    <div>
      <input type="text" placeholder="Filter by regex..."
        value={this.state.filter} onChange={ev => {
          this.props.observable.trigger({key: "filter", value: ev.target.value});
          this.setState({filter: ev.target.value});
        }} />
      {this.renderDropdown("Columns", ["LogLevel", "Time", "Tag", "Tier"], col => (
        <label>
          <input type="checkbox" checked={this.state.columns[col]} onChange={ev => {
              this.props.observable.trigger({key: "columns/" + col, value: ev.target.checked});
              this.setState({columns: Object.assign(this.state.columns, {[col]: ev.target.checked})});
            }} />
          {col}
        </label>
      ))}
      {this.renderDropdown("Log Filter", ["debug", "info", "warn", "err", "trace", "none"], lv => (
        <label>
          <input type="radio" checked={this.state.logLevel === lv} onChange={_ => {
              this.props.observable.trigger({key: "logLevel", value: lv});
              this.setState({logLevel: lv});
            }} />
          {lv}
        </label>
      ))}
      {this.renderDropdown("Set Log Level", ["debug", "info", "warn", "err", "trace", "button"], lv => {
        if (lv === "button") {
          return (<button
              style={{padding: "5px", marginLeft: "10px"}}
              onClick={_ => IrisLib.setLogLevel(this.state.setLogLevel)}>
                SET
            </button>)
        }
        else {
          return (<label>
              <input type="radio" checked={this.state.setLogLevel === lv} onChange={_ => {
                  this.setState({setLogLevel: lv});
                }} />
              {lv}
            </label>)
        }
      })}
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