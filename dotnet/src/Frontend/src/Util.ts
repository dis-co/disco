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
    return  projection ? projection(last) : last;
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