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
    var { open, name, rows, value }  = this.props.model;
    var height = open ? this.recalculateHeight(rows) : BASE_HEIGHT;

    return (
      <div className="iris-spread" ref={el => this.onMounted(el)}>
        <div className="iris-spread-child iris-flex-5"
          style={{ height: height }}>
          {/*style={{cursor: "move"}} onMouseDown={() => this.props.onDragStart()*/}
          {[<span key="0">{name}</span>]
            .concat(rows.map((kv,i) => <span key={i+1}>{kv[0] || "Label"}</span>))}      
        </div>
        <div className="iris-spread-child iris-flex-5" style={{ height: height}}>
          {[<span key="0">{`${rows[0][1]} (${rows.length})`}</span>]
            .concat(rows.map((kv,i) => <span key={i+1}>{String(kv[1])}</span>))}
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
  constructor(info) {
    this.view = View;
    this.open = false;
    this.name = info.name;
    this.rows = info.rows;
  }
}