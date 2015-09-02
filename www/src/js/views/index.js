/**
 * Combines all the view modules into one for 'qualified' import. This makes
 * life significantly easier preventing name clashes and/or long variable naming
 * schemes.
 */

module.exports.Navigation = require('./navigation');
module.exports.Project    = require('./project');
module.exports.Dashboard  = require('./dashboard');
module.exports.Patch      = require('./patch');
module.exports.CueLists   = require('./cuelists');
module.exports.Player     = require('./player');
module.exports.Components = require('./components');
module.exports.LogView    = require('./logs');
