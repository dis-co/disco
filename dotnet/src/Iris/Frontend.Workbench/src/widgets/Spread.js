import * as React from "react";
import css from "../../css/Spread.css";

const BASE_HEIGHT = 25;
const ROW_HEIGHT = 17;
// The arrow must be a bit shorter
const DIFF_HEIGHT = 2;

export default class Spread extends React.Component {
  constructor(props) {
    super(props);
    this.state = { clicked: false };
  }

  recalculateHeight() {
    return BASE_HEIGHT + (ROW_HEIGHT * this.props.rows.length);
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
    var height = clicked ? this.recalculateHeight() : BASE_HEIGHT;

    return (
      <div className="iris-spread" style={{display: "flex"}} ref={el => this.onMounted(el)}>
        {/*<div className="iris-tooltip"><div className="iris-slider"></div></div>*/}
        <div className="iris-spread-child" style={{flex: 5, height: height}}>
          {[<span key="0">Size</span>].concat(this.props.rows.map((x,i) => <span key={i+1}>{x}</span>))}
          {/*<div className="shadow"></div>
          <div className="horiz-shadow"></div>*/}
        </div>
        <div className="iris-spread-child" style={{flex: 9, height: height}}>
          {[<span key="0">{this.props.value}</span>]
            .concat(this.props.rows.map((x,i) => <span key={i+1}>{this.props.value}</span>))}
          {/*<div className="shadow"></div>
          <div className="horiz-shadow"></div>*/}
        </div>
        <div className="iris-spread-child iris-spread-end" style={{flex: 1, height: height - DIFF_HEIGHT}}>
          <img src="/img/more.png" height="7px"
            style={{transform: `rotate(${clicked ? "90" : "0"}deg)`}}
            onClick={() => {
              this.setState({ clicked: !clicked })
            }} />
          {/*<div className="scroller"></div>*/}
        </div>
      </div>
    )
  }
}
