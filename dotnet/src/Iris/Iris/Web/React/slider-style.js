let xyHandleSize="20px";
let colorActive="#2e8ece";
let colorBg= "#dddddd";
let size="20px";
let fix="4px";

.u-slider {
  "position": "relative",
  "display": "inline-block",
  "background-color": "colorBg",
  "border-radius": "3px",

  .value {
    "position": "absolute",
    "background-color": "colorActive",
    "border-radius": "3px",
  }

  .handle {
    "position": "absolute",
    "width": "size",
    "height": "size",

    &:after {
      "position": "relative",
      "display": "block",
      "content": ''
    }
  }
}

.u-slider-x,
.u-slider-y {
  .handle:after {
    "width": "size + fix",
    "height": "size + fix",
    "background-color": "#fff",
    "border": "3px solid colorActive",
    "border-radius": "50%",
  }
}

.u-slider-x {
  "height": "size",

  .handle {
    "height": "100%",
    &:after {
      "top": "-fix/2",
      left: -(size+fix)/2;
    }
  }

  .value {
    "top": "0",
    "height": "100%",
  }
}

.u-slider-y {
  "width": "size",

  .handle {
    "width": "100%",
    &:after {
      top: -(size+fix)/2;
      "left": "-fix/2",
    }
  }

  .value {
    "left": "0",
    "width": "100%",
  }
}

.u-slider-xy {
  "position": "relative",
  "width": "100%",
  "height": "100%",
  "background-color": "colorActive",
  "border-radius": "0",

  .handle {
    "position": "absolute",

    &:after {
      "position": "relative",
      "display": "block",
      "top": "-xyHandleSize/2",
      "left": "-xyHandleSize/2",
      "width": "xyHandleSize",
      "height": "xyHandleSize",
      background-color: rgba(0, 0, 0, 0);
      border: 2px solid #fff;
      "border-radius": "50%",
      content: '';
    }
  }
}