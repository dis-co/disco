var gulp    = require('gulp');

gulp.task('css', function() {
  var files = [
    './bower_components/animate.css/animate.css',
    './bower_components/bootstrap/dist/css/*.css',
    './bower_components/bootstrap-toggle/css/bootstrap-toggle.min.css',
    './bower_components/fontawesome/css/*.css',
    './bower_components/fuelux/dist/css/*.css',
    './bower_components/jquery-ui/themes/ui-lightness/**/*',
    './bower_components/spectrum/spectrum.css',
    './bower_components/select2-dist/*.css',
    './bower_components/select2-dist/*.png',
    './bower_components/select2-dist/*.gif',
    './src/css/*.css'
  ];
  gulp.src(files)
    .pipe(gulp.dest('./dist/css'));
});

gulp.task('fonts', function() {
  var files = [
    './bower_components/bootstrap/dist/fonts/*',
    './bower_components/fontawesome/fonts/*',
    './bower_components/fuelux/dist/fonts/*'
  ];
  gulp.src(files)
    .pipe(gulp.dest('./dist/fonts'));
});

gulp.task('assets', ['css', 'fonts']);
