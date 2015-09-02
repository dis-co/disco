var gulp  = require('gulp');
var shell = require('gulp-shell');

/**
 * __Build Documentation__
 *
 * The in-line source code documentation can be found in the `dist/docs` folder
 * (with the top-level file being `dist/docs.html`).
 */
gulp.task('docs', shell.task([
  'docker -o dist/docs -i src/js -n'
]));
