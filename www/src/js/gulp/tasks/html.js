var gulp = require('gulp');
var files = require('gulp-file-include');

/**
 * __Build HTML__
 */
gulp.task('html', function() {
  gulp.src(['./src/html/index.html'])
    .pipe(files({
      prefix: '@@',
      basepath: '@file'
    }))
    .pipe(gulp.dest('./dist/'));
});
