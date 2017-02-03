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

export function touchesElement(el: HTMLElement, x: number, y: number) {
  if (el != null) {
    var rect = el.getBoundingClientRect();
    return rect.left < x && x < rect.right
      && rect.top < y && y < rect.bottom;
  }
  return false;
}