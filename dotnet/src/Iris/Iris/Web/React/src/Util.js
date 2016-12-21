export function map(iterable, filter, map) {
  if (map == null) {
    map = filter;
    filter = null;
  }

  let ar = [];
  if (iterable != null) {
    let i = 0;
    for (let x of iterable)
      if (!filter || filter(x, i))
        ar.push(map(x, i++));
  }
  return ar;
}
