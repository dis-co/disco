var gulp = require('gulp');

/**
 * __Watch task (for development)__
 */
gulp.task('watch', function() {
  gulp.watch('src/css/*.css', ['assets']);
  gulp.watch('src/html/**/*.html', ['html']);
});

gulp.task('docs-watch', function() {
  gulp.watch('src/js/**/*.js', ['docs']);
});
