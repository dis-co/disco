// @ts-check

import * as $ from "jquery"
import * as React from "react"
import ContentEditable from "./widgets/ContentEditable"

export function jQueryEventAsPromise(selector, events) {
  return new Promise(resolve => {
    $(selector).on(events, e => {
      $(events).off(events);
      resolve(e);
    });
  })
}

export function raceIndexed(...promises) {
  return new Promise(resolve => {
    for (let i = 0; i < promises.length; i++) {
      promises[i].then(x => resolve([i, x]));
    }
  })
}

export function map(iterable, map) {
  let ar = [];
  if (iterable != null) {
    let i = 0, iter = iterable[Symbol.iterator](), cur = iter.next();
    while (!cur.done) {
      ar.push(map(cur.value, i++));
      cur = iter.next();
    }
  }
  return ar;
}

export function tryFirst(iterable, condition) {
  if (iterable != null) {
    let iter = iterable[Symbol.iterator](), cur = iter.next();
    if (!cur.done && condition(cur.value)) {
      return cur.value;
    }
  }
  return null;
}

export function head(iterable, projection) {
  if (iterable != null) {
    let iter = iterable[Symbol.iterator](), cur = iter.next();
    if (!cur.done) {
      return projection ? projection(cur.value) : cur.value;
    }
  }
  return null;
}

export function last(iterable, projection) {
  if (iterable != null) {
    let iter = iterable[Symbol.iterator](), cur = iter.next(), last = null;
    while (!cur.done) {
      last = cur.value;
      cur = iter.next();
    }
    return projection ? projection(last) : last;
  }
  return null;
}

export function getRandomInt(min, max) {
  min = Math.ceil(min);
  max = Math.floor(max);
  return Math.floor(Math.random() * (max - min)) + min;
}

export function touchesElement(el, x, y) {
  if (el != null) {
    var rect = el.getBoundingClientRect();
    // console.log("Touches?", {left: rect.left, right: rect.right, top: rect.top, bottom: rect.bottom}, {x, y})
    return rect.left < x && x < rect.right
      && rect.top < y && y < rect.bottom;
  }
  return false;
}

export function findParentTag(el, tagName) {
  var parent = el.parentElement;
  return parent.tagName.toUpperCase() === tagName.toUpperCase()
    ? parent : findParentTag(parent, tagName);
}

export function xand(a, b) {
  return a === b;
}

export function xor(a, b) {
  return a !== b;
}

/** Returns true if one and only one of the parameters is null */
export function oneIsNull(a, b) {
  return xor(a == null, b == null);
}

const ESCAPE_KEY = 27;
const ENTER_KEY = 13;
const RIGHT_BUTTON = 2;
const DECIMAL_DIGITS = 2;

function startDragging(posY, index, value, updater) {
    // console.log("Input drag start", index, posY)
    $(document)
        .on("contextmenu.drag", e => {
            e.preventDefault();
        })
        .on("mousemove.drag", e => {
            var diff = posY - e.clientY;
            // console.log("Input drag mouse Y diff: ", diff);
            value += diff;
            posY = e.clientY;
            if (diff !== 0)
                updater.Update(true, index, value);
        })
        .on("mouseup.drag", e => {
            updater.Update(false, index, value);
            // console.log("Input drag stop", e.clientY)
            $(document).off("mousemove.drag mouseup.drag contextmenu.drag");
        })
}

function formatValue(value, typeofValue, precision) {
    if ((typeofValue || typeof value) === "number") {
      return value.toFixed(precision == null ? DECIMAL_DIGITS : precision);
    }
    else {
      return IrisLib.toString(value);
    }
}

function getTypeofAndClass(value) {
  const typeofValue = typeof value;
  return [typeofValue, "iris-" + (typeofValue === "boolean" || typeofValue === "number" ? typeofValue : "string")];
}

export function createElement(tagName, options, value) {
    const [typeofValue, classOfValue] = getTypeofAndClass(value)
    const formattedValue = formatValue(value, typeofValue, options.precision) + (options.suffix || "");
    const props = {
      className: (options.classes || []).concat(classOfValue).join(" ")
    };

    if (options.updater != null) {
      if (typeofValue === "boolean") {
          if (options.useRightClick) {
              props.onContextMenu = (ev) => {
                  ev.preventDefault();
                  options.updater.Update(false, options.index, !value);
              }
          }
          else {
              props.onClick = (ev) => {
                  if (ev.button !== RIGHT_BUTTON)
                      options.updater.Update(false, options.index, !value);
              }
          }

          return React.createElement(tagName, props, formattedValue);
      }
      else if (typeofValue === "number") { // Numeric values, draggable
          props.onMouseDown = (ev) => {
              if (xand(ev.button === RIGHT_BUTTON, options.useRightClick))
                  startDragging(ev.clientY, options.index, value, options.updater);
          }
          if (options.useRightClick) {
              props.onContextMenu = (ev) => {
                  ev.preventDefault();
              }
          }
      }
      return React.createElement(ContentEditable, Object.assign({
        tagName: tagName,
        html: formattedValue,
        onChange(html) {
          options.updater.Update(false, options.index, html);
        }
      }, props));
    }
    else {
      return React.createElement(tagName, props, formattedValue);
    }
}