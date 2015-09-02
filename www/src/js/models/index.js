/**
 * Combines all the model modules into one for 'qualified' import. This makes
 * life significantly easier preventing name clashes and/or long variable naming
 * schemes.
 */
module.exports.Project  = require('./project.js');
module.exports.Patch    = require('./patch.js');
module.exports.Pin      = require('./pins');
module.exports.Cue      = require('./cue.js');
module.exports.CueList  = require('./cuelist.js');
module.exports.HostStat = require('./stat.js');
module.exports.FS       = require('./fs.js');
module.exports.Logs     = require('./logging.js');
module.exports.Cluster  = require('./cluster.js');

