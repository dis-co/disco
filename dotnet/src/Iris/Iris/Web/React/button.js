/// <reference path="globals.js" />

export default class Button extends React.Component {
    constructor(props) {
        super(props);
        this.props = props;
    }
    render() {
        const buttonStyle = {
            background: this.props.background || "yellow"
        }
        return <button style={buttonStyle}>Hello World!</button>;
    }
}