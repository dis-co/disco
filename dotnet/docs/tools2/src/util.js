/// @ts-check

var fs = require('fs'),
    path = require('path'),
    xml2js = require('xml2js'),
    ReactDOMServer = require('react-dom/server'),
    Handlebars = require('handlebars');

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
        fs.readFile(path.join(__dirname, '../../..', xmlDocPath), function (err, data) {
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

function parseAndDisplay(xmlDocPath) {
    parseXmlDoc(xmlDocPath)
    .then(result => {
        for (var memb of result.doc.members) {
            for (var innerMember of memb.member) {
                // console.dir(JSON.stringify(memb));
                console.log(innerMember.$.name);
                console.log(trim(innerMember.summary[0]));
                console.log("-------");
            }
        }
        console.log('Done');
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

function printPage(context, templatePath, targetPath) {
    if (context.react) {
        context.react = ReactDOMServer.renderToStaticMarkup(context.react);
    }
    var template = fs.readFileSync(templatePath).toString();
    var compiled = Handlebars.compile(template)(context);
    fs.writeFileSync(targetPath, compiled);
}

module.exports = {
    trim,
    parseXmlDoc,
    parseAndDisplay,
    parseAndGetMembersSummary,
    printPage
}
