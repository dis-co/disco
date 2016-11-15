import * as React from "react";
import Card from 'material-ui/Card/Card';
import CardActions from 'material-ui/Card/CardActions';
import CardHeader from 'material-ui/Card/CardHeader';
import CardText from 'material-ui/Card/CardText';
import FlatButton from 'material-ui/FlatButton/FlatButton';
import Table from "./Table.js"

function propsToArray(o) {
  return Object.getOwnPropertyNames(o).map(k => [k, o[k]]);
}

export default function (props) { return (
  <ul>
    {props.nodes.map(node =>
      <li>
        <Card>
          <CardHeader
            title={node.HostName}
            subtitle={node.Id.Fields[0]}
            actAsExpander={true}
            showExpandableButton={true}
          />
          <CardActions>
            <FlatButton label="Remove" />
          </CardActions>
          <CardText expandable={true}>
            <Table data={propsToArray(node)} />
          </CardText>
        </Card>
      </li>
    )}
  </ul>
)};
