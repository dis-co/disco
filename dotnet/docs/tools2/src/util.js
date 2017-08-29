/// @ts-check

var fs = require('fs');
var path = require('path');
var xml2js = require('xml2js');

function trim(txt) {
    try {
        return /^\s*(\S[\s\S]*?)\s*$/.exec(txt)[1];
    }
    catch (err) {
        console.error("Cannot trim", txt);
        return txt;
    }
}

function parseXmlDoc(xmlDocPath) {
    return new Promise(function (resolve, reject) {
        var parser = new xml2js.Parser();
        fs.readFile(xmlDocPath, function (err, data) {
            if (err) {
                reject(err);
            }
            else {
                parser.parseString(data, function (err, result) {
                    if (err) {
                        reject(err);
                    }
                    else {
                        resolve(result);
                    }
                });
            }
        });
    });
}

function parseAndGetMembersSummary(xmlDocPath) {
    return parseXmlDoc(xmlDocPath)
    .then(result => {
        var members = result.doc.members[0].member;
        var ar = new Array(members.length);
        for (var i = 0; i < ar.length; i++) {
            var m = members[i]
            ar[i] = [m.$.name, trim(m.summary[0])];
        }
        return ar;
    });
}

function getDirectoryFiles(dir, isRecursive) {
    var items = fs.readdirSync(dir);
    var files = [];
    for (var name of items) {
        var item = path.join(dir, name);
        if (fs.lstatSync(item).isDirectory()) {
            if (isRecursive) {
                files = files.concat(getDirectoryFiles(item, true));
            }
        }
        else {
            files.push(item);
        }
    }
    return files;
}

module.exports = {
    trim,
    parseXmlDoc,
    parseAndGetMembersSummary,
}
