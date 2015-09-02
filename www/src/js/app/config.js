/**
 * _Global Configuration_
 */
module.exports = {
  global: {
    sync: {
      /**
       * Names of the global push/pull update channels used in the sync adapter
       * and by every model that taps into those data streams.
       *
       * - `pull` is the regular query channel for client side data structures
       *   to implement their CRUD methods against
       * 
       * - `push` is the channel that models and collections subscribe to, which
       *   delievers (pushes) changes on server-side data structures to all
       *   interested parties.
       */
      channels: {
        push: 'push',
        pull: 'pull'
      }
    }
  }
};
