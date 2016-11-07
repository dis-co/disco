/// <reference path="globals.js" />

class Form extends React.Component {
  constructor(props) {
    super(props);
    this.state = {value: ''};
  }

  handleChange(name, event) {
    let change = {};
    change[name] = event.target.value;
    this.setState(change);
  }

  handleSubmit(event) {
    require([this.state.widgetName], (com) => {
        fetch("data/" + this.state.widgetData + ".json")
        .then(res => res.json())
        .then(json => this.setState({widget: React.createElement(com.default, json)}))
        .catch(err => console.log(err));
    });
  }

  render() {
    const el = this.state.widget;
    return (
      <div>
        <div>
            <input type="text"
                placeholder="Wiget name"
                value={this.state.widgetName}
                onChange={this.handleChange.bind(this, "widgetName")} />
            <input type="text"
                placeholder="Data file"
                value={this.state.widgetData}
                onChange={this.handleChange.bind(this, "widgetData")} />
            <button onClick={this.handleSubmit.bind(this)}>
            Load widget
            </button>
        </div>
        {el}
      </div>
    );
  }
}

ReactDOM.render(
  <Form />,
  document.getElementById('app')
);