/**
 * Combines all the components into one for 'qualified' import. This makes
 * life significantly easier preventing name clashes and/or long variable naming
 * schemes.
 */

module.exports.Config     = require('./config.js');
module.exports.Iris       = require('./iris.js');
module.exports.Router     = require('./router.js');
