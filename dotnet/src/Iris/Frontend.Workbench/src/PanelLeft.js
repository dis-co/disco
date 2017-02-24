import React, { Component } from 'react'
import css from "../css/PanelLeft.less";
import Log from "./widgets/Log";

function cardClicked(title, model) {
  switch (title.toUpperCase()) {
    case "LOG":
      model.addWidget(Log);
      break;
    default:
      alert("Widget " + title + " is not currently supported")
  }
}

const Card = props => (
  <div className="iris-panel-left-child" onClick={() => cardClicked(props.title, props.model)}>
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
        <Card key={0} model={this.props.model} letter="P" title="Project Overview (Big)" text="Cluster Settings" />
        <Card key={1} model={this.props.model} letter="P" title="Project Overview (Small)" text="Cluster Settings" />
        <Card key={2} model={this.props.model} letter="B" title="Branches" text="Cluster Settings" />
        <Card key={3} model={this.props.model} letter="L" title="LOG" text="Cluster Settings" />
        <Card key={4} model={this.props.model} letter="U" title="User Management" text="Cluster Settings" />
        <Card key={5} model={this.props.model} letter="H" title="Unassigned Hosts" text="Cluster Settings" />
        <Card key={6} model={this.props.model} letter="R" title="Remotter" text="Cluster Settings" />
        <Card key={7} model={this.props.model} letter="S" title="Project Settings" text="Cluster Settings" />
        <Card key={8} model={this.props.model} letter="L" title="Library" text="Graph View" />
      </div>
    )
  }
}
