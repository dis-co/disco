import * as React from "react";
import css from "../css/PanelLeft.less";

function cardClicked(title) {
  console.log(title);
}

const Card = props => (
  <div className="iris-panel-left-child" onClick={() => cardClicked(props.title)}>
    <div>{props.letter}</div>
    <div>
      <p><strong>{props.title}</strong></p>
      <p>{props.text}</p>
    </div>
  </div>
);

export default class PanelLeft extends React.Component {
  constructor(props) {
    super(props);
  }

  render() {
    return (
      <div className="iris-panel-left">
        <Card key={0} letter="P" title="Project Overview (Big)" text="Cluster Settings" />
        <Card key={1} letter="P" title="Project Overview (Small)" text="Cluster Settings" />
        <Card key={2} letter="B" title="Branches" text="Cluster Settings" />
        <Card key={3} letter="L" title="LOG" text="Cluster Settings" />
        <Card key={4} letter="U" title="User Management" text="Cluster Settings" />
        <Card key={5} letter="H" title="Unassigned Hosts" text="Cluster Settings" />
        <Card key={6} letter="R" title="Remotter" text="Cluster Settings" />
        <Card key={7} letter="S" title="Project Settings" text="Cluster Settings" />
        <Card key={8} letter="L" title="Library" text="Graph View" />
      </div>
    )
  }
}
