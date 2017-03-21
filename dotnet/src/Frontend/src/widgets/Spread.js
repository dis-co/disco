import * as React from "react";
import css from "../../css/Spread.less";

const BASE_HEIGHT = 25;
const ROW_HEIGHT = 17;
// The arrow must be a bit shorter
const DIFF_HEIGHT = 2;

const ESCAPE_KEY = 27;
const ENTER_KEY = 13;

class View extends React.Component {
  constructor(props) {
    super(props);
    this.state = {editIndex: -1};
  }

  recalculateHeight(rows) {
    return BASE_HEIGHT + (ROW_HEIGHT * rows.length);
  }

  onMounted(el) {
    if (el == null)
      return;

    $(el).resizable({
      minWidth: 150,
      handles: "e",
      resize: function(event, ui) {
          ui.size.height = ui.originalSize.height;
      }
    });
  }

  handleSubmit(event) {
    console.log("Submitting. Zooommm....", event.target.value)
  
  }

  handleKeyDown(event, index, model) {
    if (event.which === ESCAPE_KEY) {
      this.setState({editIndex: -1});
    } else if (event.which === ENTER_KEY) {
      model.update(index, event.target.value);
      this.setState({editIndex: -1});
    }
  }

  renderRowLabels(model) {
    var viewRows = [];
    // {/*style={{cursor: "move"}} onMouseDown={() => this.props.onDragStart()*/}
    viewRows.push(<span key="0">{model.pin.Name}</span>)
    model.rows.forEach((kv,i) => {
      viewRows.push(<span key={i+1}>{kv[0] || "Label"}</span>)
    });
    return viewRows;
  }

  renderRowValues(model) {
    var viewRows = [];
    viewRows.push(<span key="0">{`${model.rows[0][1]} (${model.rows.length})`}</span>)
    model.rows.forEach((kv,i) => {
      viewRows.push(this.state.editIndex === i
        ? <input
            key={i+1}
            value={this.state.editText}
            onBlur={ev => this.setState({editIndex: -1})}
            onChange={ev => this.setState({editText: ev.target.value})}
            onKeyDown={ev => this.handleKeyDown(ev, i, model)}
          />
        : <span
            key={i+1}
            onDoubleClick={ev => this.setState({editIndex: i, editText: String(kv[1])})}
          >
            {String(kv[1])}
          </span>
      )
    });
    return viewRows;
  }

  render() {
    var model = this.props.model;
    var height = open ? this.recalculateHeight(model.rows) : BASE_HEIGHT;

    return (
      <div className="iris-spread" ref={el => this.onMounted(el)}>
        <div className="iris-spread-child iris-flex-1" style={{ height: height }}>
          {this.renderRowLabels(model)}
        </div>
        <div className="iris-spread-child iris-flex-2" style={{ height: height}}>
          {this.renderRowValues(model)}
        </div>
        <div className="iris-spread-child iris-spread-end" style={{ height: height - DIFF_HEIGHT}}>
          <img src="/img/more.png" height="7px"
            style={{transform: `rotate(${model.open ? "90" : "0"}deg)`}}
            onClick={ev => {
              ev.stopPropagation();
              this.props.model.open = !this.props.model.open;
              this.forceUpdate();
            }} />
        </div>
      </div>
    )
  }
}

export default class Spread {
  constructor(pin) {
    this.view = View;
    this.pin = pin;
    this.open = false;
    this.rows = Iris.pinToKeyValuePairs(pin);
  }

  update(rowIndex, newValue) {
    debugger;
    Iris.updateSlices(this.pin, rowIndex, newValue);
  }
}