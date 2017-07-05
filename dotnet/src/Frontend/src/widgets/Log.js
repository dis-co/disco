import React, { Component } from 'react'
import {Table, Column, Cell} from 'fixed-data-table'
import "fixed-data-table/dist/fixed-data-table.css"
import { map } from "../Util.ts"

class View extends Component {
  constructor(props) {
    super(props);
    this.state = { logs: props.global.state.logs };
  }

  componentDidMount() {
    this.disposable =
      this.props.global.subscribe("logs", logs => {
        this.setState({logs});
      });
  }

  componentWillUnmount() {
    if (this.disposable) {
      this.disposable.dispose();
    }
  }

  render() {
   return (
    <Table
      rowsCount={this.state.logs.length}
      rowHeight={30}
      headerHeight={30}
      width={800}
      height={600}
      >
        <Column width={100} header={<Cell>LogLevel</Cell>} cell={({rowIndex, ...props}) => (
            <Cell {...props}>{IrisLib.toString(this.state.logs[rowIndex].LogLevel)}</Cell>
        )} />
        <Column width={100} header={<Cell>Time</Cell>} cell={({rowIndex, ...props}) => (
            <Cell {...props}>{new Date(this.state.logs[rowIndex].Time).toLocaleTimeString()}</Cell>
        )} />
        <Column width={100} header={<Cell>Tag</Cell>} cell={({rowIndex, ...props}) => (
            <Cell {...props}>{this.state.logs[rowIndex].Tag}</Cell>
        )} />
        <Column width={100} header={<Cell>Tier</Cell>} cell={({rowIndex, ...props}) => (
            <Cell {...props}>{IrisLib.toString(this.state.logs[rowIndex].Tier)}</Cell>
        )} />
        <Column width={400} header={<Cell>Message</Cell>} cell={({rowIndex, ...props}) => (
            <Cell {...props} style={{whiteSpace: "nowrap"}}>
              {this.state.logs[rowIndex].Message}
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
    this.layout = {
      x: 0, y: 0,
      w: 8, h: 6,
      minW: 2, maxW: 20,
      minH: 2, maxH: 20
    }
  }
}