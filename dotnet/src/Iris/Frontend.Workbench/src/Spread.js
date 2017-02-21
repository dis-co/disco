import * as React from "react";

const BASE_HEIGHT = 25;
const ROW_HEIGHT = 17;
// The arrow must be a bit shorter
const DIFF_HEIGHT = 2;

export default class Spread extends React.Component {
  constructor(props) {
    super(props);
    this.click = false;
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
    var height = this.click ? this.recalculateHeight() : BASE_HEIGHT;

    return (
      <div className="parent eg" style={{display: "flex"}} ref={el => this.onMounted(el)}>
        <div className="tooltip"><div id="slider"></div></div>
        <div className="child re" style={{flex: 7, height: height}}>
          {[<span key="0">Size</span>].concat(this.props.rows.map((x,i) => <span key={i+1}>{x}</span>))}
          {/*<div className="shadow"></div>
          <div className="horiz-shadow"></div>*/}
        </div>
        <div className="child re" style={{flex: 7, height: height}}>
          {[<span key="0">{this.props.value}</span>]
            .concat(this.props.rows.map((x,i) => <span key={i+1}>{this.props.value}</span>))}
          {/*<div className="shadow"></div>
          <div className="horiz-shadow"></div>*/}
        </div>
        <div className="child end" style={{flex: 1, height: height - DIFF_HEIGHT}}>
          <img src="/img/more.png" height="7px" onClick={() => {
            if (!this.click) {
              var height = this.recalculateHeight(); 
              $(".child.re").css("height",`${height}px`);
              $(".end").css("height",`${height - DIFF_HEIGHT}px`);
              $(".end img").css("transform","rotate(90deg)");
              this.click = true;
            } else {
              $(".child.re").css("height",`${BASE_HEIGHT}px`);
              $(".end").css("height",`${BASE_HEIGHT - DIFF_HEIGHT}px`);
              $(".end img").css("transform","rotate(0deg)");
              this.click = false;
            }
          }} />
          <div className="scroller"></div>
        </div>
      </div>
    )
  }
}
