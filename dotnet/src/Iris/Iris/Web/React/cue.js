export default class Cue extends React.Component {
    constructor(props) {
        super(props);
        this.props = props;
    }
    render() {
        const cueStyle = {
            border: "2px solid #888",
            minHeight: "30px",
            minWidth: "80px"
        };
        const ulStyle = {
            listStyle: "none"
        };
        return <div class="cue"
                    style={cueStyle}
                    onClick={ev => window.alert(this.props.message)}>
            <ul style={ulStyle}>
                <li>
                    <strong>Id:</strong>
                    <div id="id">{this.props.id}</div>
                </li>
                <li>
                    <strong>Name:</strong>
                    <div id="name">{this.props.name}</div>
                </li>
                <li>
                    <button id="play">Play</button>
                </li>
            </ul>
        </div>
    }
}