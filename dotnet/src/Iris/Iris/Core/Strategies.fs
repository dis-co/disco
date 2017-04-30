namespace Iris.Core

// * DispatchStrategy

type DispatchStrategy =
  | Replicate
  | Publish
  | Resolve

// * PersistenceStrategy

type PersistenceStrategy =
  | Volatile
  | Durable
