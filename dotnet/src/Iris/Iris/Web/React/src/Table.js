import * as React from "react";
import Table from 'material-ui/Table/Table';
import TableBody from 'material-ui/Table/TableBody';
import TableRow from 'material-ui/Table/TableRow';
import TableRowColumn from 'material-ui/Table/TableRowColumn';

const config = {
    fixedHeader: false,
    fixedFooter: false,
    stripedRows: false,
    showRowHover: true,
    selectable: false,
    multiSelectable: false,
    enableSelectAll: false,
    deselectOnClickaway: true,
    showCheckboxes: false,
    height: '300px',
};

export default function (props) { return (
  <div>
    <Table
        height={config.height}
        fixedHeader={config.fixedHeader}
        fixedFooter={config.fixedFooter}
        selectable={config.selectable}
        multiSelectable={config.multiSelectable}
    >
        <TableBody
        displayRowCheckbox={config.showCheckboxes}
        deselectOnClickaway={config.deselectOnClickaway}
        showRowHover={config.showRowHover}
        stripedRows={config.stripedRows}
        >
        {props.data.map( (kv, index) => (
          <TableRow key={index}>
            <TableRowColumn>{kv[0]}</TableRowColumn>
            <TableRowColumn>{String(kv[1])}</TableRowColumn>
          </TableRow>
        ))}
        </TableBody>
    </Table>
  </div>
)};

//   <TableHeader
//     displaySelectAll={config.showCheckboxes}
//     adjustForCheckbox={config.showCheckboxes}
//     enableSelectAll={config.enableSelectAll}
//   >
//     <TableRow>
//       <TableHeaderColumn colSpan="3" tooltip="Header" style={{textAlign: 'center'}}>
//         Super Header
//       </TableHeaderColumn>
//     </TableRow>
//     <TableRow>
//       <TableHeaderColumn tooltip="Key">Key</TableHeaderColumn>
//       <TableHeaderColumn tooltip="Value">Value</TableHeaderColumn>
//     </TableRow>
//   </TableHeader>

// <TableFooter
// adjustForCheckbox={config.showCheckboxes}
// >
// <TableRow>
//     <TableRowColumn>Key</TableRowColumn>
//     <TableRowColumn>Value</TableRowColumn>
// </TableRow>
// <TableRow>
//     <TableRowColumn colSpan="3" style={{textAlign: 'center'}}>Footer</TableRowColumn>
// </TableRow>
// </TableFooter>
