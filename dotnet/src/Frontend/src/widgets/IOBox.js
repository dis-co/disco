import * as React from "react";
import css from "../../css/Spread.css";

class View extends React.Component {
  constructor(props) {
    super(props);
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
    var { name, value }  = this.props.model;
    
    return (
      <div className="iris-spread" ref={el => this.onMounted(el)}>
        <div className="iris-spread-child iris-flex-5">
          <span>{name}</span>
        </div>
        <div className="iris-spread-child iris-flex-5">
          <span>{value.toString()}</span>
        </div>
        <div className="iris-spread-child iris-spread-end">
        </div>
      </div>
    )
  }
}

export default class IOBox {
  constructor(name, value) {
    this.view = View;
    this.name = name;
    this.value = value;
  }
}