import * as React from "react";
import { ResizableBox } from 'react-resizable';

export class IOBox extends React.Component {
    constructor(props) {
        super(props);
        this.state = { showBody: false }
    }

    renderBody() {
        if (this.state.showBody) {
            return (
                <tbody>
                    <tr><td>My value</td><td>100</td></tr>
                    <tr><td>My value</td><td>200</td></tr>
                </tbody>
            )
        }
        else {
            return null;
        }
    }

    render() {
        return (
            <table className="iris-iobox-table" ref={el => {
                if (el != null) {
                    $(".iris-iobox-table th").resizable();
                }
            }}>
                <thead onClick={ev => this.setState({ showBody: !this.state.showBody }) }>
                    <tr>
                        <th>My value</th>
                        <th>1000</th>
                    </tr>
                </thead>
                {this.renderBody()}
            </table>
        )
    }
}
