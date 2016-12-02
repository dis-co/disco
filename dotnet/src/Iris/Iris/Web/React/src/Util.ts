export function map<K,V>(iterable: Iterable<K>, f: (x:K)=>V): V[] {
  let ar: V[] = [];
  if (iterable != null)
    for (let x of iterable)
      ar.push(f(x));
  return ar;
}

