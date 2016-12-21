export function object2tree(obj, key, f) {
  let f2 = typeof f === "function"
    ? (k,v) => {
      let m = f(k,v);
      return m != null ? m : makeParentNode(k, v);
    }
    : makeParentNode;

  function makeParentNode(k, v) {
    return Array.isArray(v)
      ? { module: `${k}: ${v.join()}`}
      : { module: k, children: makeChildren(v) };
  }

  function makeChildren(obj) {
    let children = [];
    Object.getOwnPropertyNames(obj).forEach(k => {
      let v = obj[k];
      if (v != null && typeof v != "function") {
        children.push(typeof v === "object"
          ? f2(k,v)
          : { module: `${k}: ${v}`})
      }
    })
    return children;
  }

  return { module: key, children: makeChildren(obj) };
}

export const sample = {module: 'Views', children: [
{
  module: 'View 1',
  children: [
    {
      module: 'Widget 1',
      collapsed: true,
      children: [
        {module: 'IN'},
        {module: 'OUT', children: [
          {module:'Value'},
          {module:'Value'}
        ]}
      ]
    },
    {
      module: 'Widget 2',
      collapsed: true,
      children: [
        {module: 'IN'},
        {module: 'OUT', children: [
          {module:'Value'},
          {module:'Value'}
        ]}
      ]
    },
    {
      module: 'Widget 3',
      children: [
        {module: 'IN'},
        {module: 'OUT', children: [
          {module:'Value'},
          {module:'Value'}
        ]}
      ]
    }
  ]
},
{
  module: 'View 2',
  children: [
    {
      module: 'Widget 1',
      collapsed: true,
      children: [
        {module: 'IN'},
        {module: 'OUT', children: [
          {module:'Value'},
          {module:'Value'}
        ]}
      ]
    },
    {
      module: 'Widget 2',
      collapsed: true,
      children: [
        {module: 'IN'},
        {module: 'OUT', children: [
          {module:'Value'},
          {module:'Value'}
        ]}
      ]
    },
    {
      module: 'Widget 3',
      children: [
        {module: 'IN', leaf: true},
        {module: 'OUT', children: [
          {module:'Value'},
          {module:'Value'}
        ]}
      ]
    }
  ]
},
{
  module: 'View 3',
  children: [
    {
      module: 'Widget 1',
      collapsed: true,
      children: [
        {module: 'IN'},
        {module: 'OUT', children: [
          {module:'Value'},
          {module:'Value'}
        ]}
      ]
    },
    {
      module: 'Widget 2',
      collapsed: true,
      children: [
        {module: 'IN'},
        {module: 'OUT', children: [
          {module:'Value'},
          {module:'Value'}
        ]}
      ]
    },
    {
      module: 'Widget 3',
      children: [
        {module: 'IN'},
        {module: 'OUT', children: [
          {module:'Value'},
          {module:'Value'}
        ]}
      ]
    }
  ]
},
]};