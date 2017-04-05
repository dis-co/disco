import React, { Component } from 'react'
import css from "../css/PanelLeft.less"
import Log from "./widgets/Log"
import Manager from "./widgets/Manager"
import GraphView from "./widgets/GraphView"
import CueList from "./widgets/CueList"

function cardClicked(title, global) {
  switch (title.toUpperCase()) {
    case "LOG":
      global.addWidget(new Log());
      break;
    case "MANAGER":
      global.addWidget(new Manager());
      break;
    case "GRAPH VIEW":
      global.addWidget(new GraphView());
      break;
    case "CUE LIST":
      global.addWidget(new CueList());
      break;      
    default:
      alert("Widget " + title + " is not currently supported")
  }
}

const Card = props => (
  <div className="iris-panel-left-child" onClick={() => cardClicked(props.title, props.global)}>
    <div>{props.letter}</div>
    <div>
      <p><strong>{props.title}</strong></p>
      <p>{props.text}</p>
    </div>
  </div>
);

export default class PanelLeft extends Component {
  constructor(props) {
    super(props);
  }

  render() {
    return (
      <div className="iris-panel-left">
        <Card key={0} global={this.props.global} letter="L" title="LOG" text="Cluster Settings" />
        <Card key={1} global={this.props.global} letter="G" title="Graph View" text="Cluster Settings" />
        <Card key={2} global={this.props.global} letter="C" title="Cue List" text="Cluster Settings" />
        <Card key={3} global={this.props.global} letter="M" title="Manager" text="Cluster Settings" />
        <Card key={4} global={this.props.global} letter="P" title="Project Overview (Small)" text="Cluster Settings" />
        <Card key={5} global={this.props.global} letter="B" title="Branches" text="Cluster Settings" />
        <Card key={6} global={this.props.global} letter="U" title="User Management" text="Cluster Settings" />
        <Card key={7} global={this.props.global} letter="H" title="Unassigned Hosts" text="Cluster Settings" />
        <Card key={8} global={this.props.global} letter="R" title="Remotter" text="Cluster Settings" />
        <Card key={9} global={this.props.global} letter="S" title="Project Settings" text="Cluster Settings" />
        <Card key={10} global={this.props.global} letter="L" title="Library" text="Graph View" />
        <Card key={11} global={this.props.global} letter="P" title="Project Overview (Big)" text="Cluster Settings" />
      </div>
    )
  }
}
