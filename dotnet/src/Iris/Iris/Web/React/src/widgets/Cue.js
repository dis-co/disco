import * as React from "react";
import Panel from 'muicss/lib/react/panel';
import { map } from "../Util";

export default function WidgetCue(props) {
  return (
    <Panel className="panel-cue">
      {map(props.cue.Pins, x =>
        <table className="mui-table mui-table--bordered">
          <tbody>
            {map(x.Slices.Fields[0], (y,i) =>
              <tr key={i}>
                <td>{x.Name}</td>
                <td>{y.Value}</td>
              </tr>
            )}
          </tbody>
        </table>
      )}
    </Panel>
  )
}
