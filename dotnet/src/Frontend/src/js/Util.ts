import * as $ from "jquery"
import * as React from "react"
import ContentEditable from "./widgets/ContentEditable"

export function jQueryEventAsPromise(selector: any, events: string) {
  return new Promise<JQuery.Event<HTMLElement,null>>(resolve => {
    $(selector).on(events, e => {
      $(events).off(events);
      resolve(e);
    });
  })
}

export function raceIndexed(...promises: Promise<any>[]) {
  return new Promise<[number, any]>(resolve => {
    for (let i = 0; i < promises.length; i++) {
      promises[i].then(x => resolve([i, x]));
    }
  })
}

export function map<T,U>(iterable: Iterable<T>, map: (x:T,i?:number)=>U) {
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

export function tryFirst<T>(iterable: Iterable<T>, condition?: (x:T)=>boolean) {
  if (iterable != null) {
    let iter = iterable[Symbol.iterator](), cur = iter.next();
    if (!cur.done && condition(cur.value)) {
      return cur.value;
    }
  }
  return null;
}

export function head<T,U>(iterable: Iterable<T>, projection?: (x:T)=>U) {
  if (iterable != null) {
    let iter = iterable[Symbol.iterator](), cur = iter.next();
    if (!cur.done) {
      return projection ? projection(cur.value) : cur.value;
    }
  }
  return null;
}

export function last<T,U>(iterable: Iterable<T>, projection?: (x:T)=>U) {
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

export function getRandomInt(min: number, max: number) {
  min = Math.ceil(min);
  max = Math.floor(max);
  return Math.floor(Math.random() * (max - min)) + min;
}

export function touchesElement(el: HTMLElement, x: number, y: number) {
  if (el != null) {
    var rect = el.getBoundingClientRect();
    // console.log("Touches?", {left: rect.left, right: rect.right, top: rect.top, bottom: rect.bottom}, {x, y})
    return rect.left < x && x < rect.right
      && rect.top < y && y < rect.bottom;
  }
  return false;
}

export function findParentTag(el: HTMLElement, tagName: string): HTMLElement {
  var parent = el.parentElement;
  return parent.tagName.toUpperCase() === tagName.toUpperCase()
    ? parent : findParentTag(parent, tagName);
}

export function xand(a: boolean, b: boolean) {
  return a === b;
}

export function xor(a: boolean, b: boolean) {
  return a !== b;
}

/** Returns true if one and only one of the parameters is null */
export function oneIsNull(a: {}, b: {}) {
  return xor(a == null, b == null);
}

const ESCAPE_KEY = 27;
const ENTER_KEY = 13;
const RIGHT_BUTTON = 2;
const DECIMAL_DIGITS = 2;

interface IUpdater {
    Update(dragging: boolean, index: number, value: any): void;
}

function startDragging(posY: number, index: number, value: number, updater: IUpdater) {
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

export function formatValue(value: any) {
    return typeof value === "number" ? value.toFixed(DECIMAL_DIGITS) : String(value);
}

export function addInputView(index: number, value: any, tagName, useRightClick: boolean, updater: IUpdater) {

    let typeofValue = typeof value,
        props = {} as any, //{ key: index } as any,
        formattedValue = formatValue(value);

    // Boolean values, not editable
    if (typeofValue === "boolean") {
        if (useRightClick) {
            props.onContextMenu = (ev: React.MouseEvent<HTMLElement>) => {
                ev.preventDefault();
                updater.Update(false, index, !value);
            }
        }
        else {
            props.onClick = (ev: React.MouseEvent<HTMLElement>) => {
                if (ev.button !== RIGHT_BUTTON)
                    updater.Update(false, index, !value);
            }
        }

        return React.createElement(tagName, props, formattedValue);
    }

    // Numeric values, draggable
    if (typeofValue === "number") {
        props.onMouseDown = (ev: React.MouseEvent<HTMLElement>) => {
            if (xand(ev.button === RIGHT_BUTTON, useRightClick))
                startDragging(ev.clientY, index, value, updater);
        }
        if (useRightClick) {
            props.onContextMenu = (ev: React.MouseEvent<HTMLElement>) => {
                ev.preventDefault();
            }
        }
    }

    return React.createElement(ContentEditable, Object.assign({
      tagName: tagName,
      html: formattedValue,
      onChange(html) {
        updater.Update(false, index, html);
      }
    }, props));
}