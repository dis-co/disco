export function map<T,U>(xs: Iterable<T>, f:(x:T)=>U) {
  let ar: U[] = [];
  for (const x of xs)
    ar.push(f(x))
  return ar;
}