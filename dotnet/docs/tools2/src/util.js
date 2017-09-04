/// @ts-check

var fs = require('fs');
var path = require('path');
var xml2js = require('xml2js');

function parseXml(xmlPath) {
    return new Promise(function (resolve, reject) {
        var parser = new xml2js.Parser();
        fs.readFile(xmlPath, function (err, data) {
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

function parseXmlDocAndGetMembers(xmlPath) {
    return parseXml(xmlPath)
    .then(result => {
        var members = result.doc.members[0].member;
        var ar = new Array(members.length);
        for (var i = 0; i < members.length; i++) {
            var dic = new Map();
            var m = members[i];
            dic.set("name", [m.$.name]);
            Object.keys(m).forEach(k => {
                if (k !== "$")
                    dic.set(k, m[k]);
            })
            ar.push(dic);
        }
        return ar;
    });
}

module.exports = {
    parseXml,
    parseXmlDocAndGetMembers,
}
