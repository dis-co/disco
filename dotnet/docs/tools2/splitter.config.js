/// @ts-check
const util = require("./splitter.util.js");

module.exports = {
  entry: util.resolve("src/XmlParser.fsproj"),
  outDir: util.resolve("build"),
  babel: { plugins: ["transform-es2015-modules-commonjs"] },
  fable: { define: ["DEBUG"] },
  postbuild() {
    util.runCommand(".", "node build/Main.js")
  }
};
