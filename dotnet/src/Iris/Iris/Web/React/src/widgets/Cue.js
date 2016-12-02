import * as React from "react";
import Panel from 'muicss/lib/react/panel';
import { map } from "../Util";

export default function WidgetCue(props) {
  return (
    <Panel className="panel-cue">
      {map(props.cue.IOBoxes, x =>
        <table className="mui-table mui-table--bordered">
          <tbody>
            <tr>
              <td>{x.Name}</td>
              <td>{x.Value}</td>
            </tr>
          </tbody>
        </table>
      )}
    </Panel>
  )
}
