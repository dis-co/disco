export function map(iterable, f) {
  let ar = [];
  if (iterable != null) {
    let i = 0;
    for (let x of iterable)
      ar.push(f(x, i++));
  }
  return ar;
}
