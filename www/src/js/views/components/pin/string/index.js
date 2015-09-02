var Basic = require('./basic.js');
var FS    = require('./fs.js');

module.exports.String    = Basic.String;
module.exports.MultiLine = Basic.MultiLine;
module.exports.Url       = Basic.Url;
module.exports.IP        = Basic.IP;
module.exports.FileName  = FS.FileName;  
module.exports.Directory = FS.Directory; 

