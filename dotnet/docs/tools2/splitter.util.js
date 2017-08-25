/// @ts-check
const path = require("path");
const child_process = require("child_process");

function resolve(filePath) {
  return path.resolve(__dirname, filePath)
}

function splitByWhitespace(str) {
    function stripQuotes(str, start, end) {
        return str[start] === '"' && str[end - 1] === '"'
                ? str.substring(start + 1, end - 1)
                : str.substring(start, end);
    }
    var reg = /\s+(?=([^"]*"[^"]*")*[^"]*$)/g;
    reg.lastIndex = 0;
    var tmp, tmp2, results = [], lastIndex = 0;
    while ((tmp = reg.exec(str)) !== null) {
        results.push(stripQuotes(str, lastIndex, tmp.index));
        lastIndex = tmp.index + tmp[0].length;
    }
    results.push(stripQuotes(str, lastIndex, str.length));
    return results;
}

function runCommandPrivate(workingDir, command, continuation) {
    var cmd, args;
    console.log(workingDir + "> " + command + "\n");
    // If there's no continuation, it means the process will run in parallel (postbuild-once).
    // If we use `cmd /C` on Windows we won't be able to kill the cmd child process later.
    // See http://stackoverflow.com/a/32814686 (unfortunately the solutions didn't seem to apply here)
    if (typeof process === "object" && process.platform === "win32" && continuation) {
        cmd = "cmd";
        args = splitByWhitespace(command);
        args.splice(0,0,"/C");
    }
    else {
        args = splitByWhitespace(command);
        cmd = args[0];
        args = args.slice(1);
    }
    var proc = child_process.spawn(cmd, args, { cwd: workingDir });
    proc.on('exit', function(code) {
        if (continuation) {
            code === 0 ? continuation.resolve(code) : continuation.reject(code);
        }
    });
    proc.stderr.on('data', function(data) {
        console.error(data.toString());
    });
    proc.stdout.on("data", function(data) {
        console.log(data.toString());
    });
    return proc;
}

/** Runs a command and returns a Promise, requires child_process */
function runCommand(workingDir, command) {
    return new Promise(function (resolve, reject) {
        runCommandPrivate(workingDir, command, { resolve, reject })
    });
}

module.exports = {
    resolve,
    runCommand
}