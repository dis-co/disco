import * as React from "react";
import css from "../../css/Spread.css";

const BASE_HEIGHT = 25;
const ROW_HEIGHT = 17;
// The arrow must be a bit shorter
const DIFF_HEIGHT = 2;

class View extends React.Component {
  constructor(props) {
    super(props);
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

  render() {
    var { open, rows, value }  = this.props.model;
    var height = open ? this.recalculateHeight(rows) : BASE_HEIGHT;

    return (
      <div className="iris-spread" ref={el => this.onMounted(el)}>
        <div className="iris-spread-child iris-flex-5"
          style={{ height: height }}>
          {[<span key="0" style={{cursor: "move"}} onMouseDown={() => this.props.onDragStart()}>Size</span>]
            .concat(rows.map((x,i) => <span key={i+1}>{x}</span>))}
        </div>
        <div className="iris-spread-child iris-flex-9" style={{ height: height}}>
          {[<span key="0">{value}</span>]
            .concat(rows.map((x,i) => <span key={i+1}>{value}</span>))}
        </div>
        <div className="iris-spread-child iris-spread-end" style={{ height: height - DIFF_HEIGHT}}>
          <img src="/img/more.png" height="7px"
            style={{transform: `rotate(${open ? "90" : "0"}deg)`}}
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
  constructor() {
    this.view = View;
    this.open = false;
    this.rows = [1,2,3,4,5];
    this.value = "W: 1920, H: 1080";
  }
}