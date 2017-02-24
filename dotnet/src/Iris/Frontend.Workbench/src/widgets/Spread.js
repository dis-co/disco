import * as React from "react";
import css from "../../css/Spread.css";

const BASE_HEIGHT = 25;
const ROW_HEIGHT = 17;
// The arrow must be a bit shorter
const DIFF_HEIGHT = 2;

export default class Spread extends React.Component {
  constructor(props) {
    super(props);
    this.state = {
      clicked: false,
      rows: [1,2,3,4,5],
      value: "W: 1920, H: 1080"
    };
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
    var clicked = this.state.clicked;
    var rows = this.state.rows, value = this.state.value;
    var height = clicked ? this.recalculateHeight(rows) : BASE_HEIGHT;

    return (
      <div className="iris-spread" ref={el => this.onMounted(el)}>
        <div className="iris-spread-child iris-flex-5" style={{ height: height}}>
          {[<span key="0">Size</span>].concat(rows.map((x,i) => <span key={i+1}>{x}</span>))}
        </div>
        <div className="iris-spread-child iris-flex-9" style={{ height: height}}>
          {[<span key="0">{value}</span>]
            .concat(rows.map((x,i) => <span key={i+1}>{value}</span>))}
        </div>
        <div className="iris-spread-child iris-spread-end" style={{ height: height - DIFF_HEIGHT}}>
          <img src="/img/more.png" height="7px"
            style={{transform: `rotate(${clicked ? "90" : "0"}deg)`}}
            onClick={() => {
              this.setState({ clicked: !clicked })
            }} />
        </div>
      </div>
    )
  }
}
