var gulp = require('gulp');
var gutil = require('gulp-util');
var sourcemaps = require('gulp-sourcemaps');
var source = require('vinyl-source-stream');
var buffer = require('vinyl-buffer');
var notify = require('gulp-notify');
var watchify = require('watchify');
var browserify = require('browserify');
var debowerify = require('debowerify');

var bundler = watchify(browserify([
  './src/js/shims/jquery.js',
  './src/js/shims/backbone.js',
  './src/js/shims/marionette.js',
  './src/js/main.js'
], watchify.args));

bundler.transform('debowerify');
bundler.transform('browserify-handlebars');

bundler.on('update', bundle); // on any dep update, runs the bundler

function bundle() {
  return bundler.bundle()
    // log errors if they happen
    .on('error', gutil.log.bind(gutil, 'Browserify Error'))
    .on('error', notify.onError(function (error) {
        return "Build failed: " + error.message;
      }))
    .pipe(source('iris.js'))
    // optional, remove if you dont want sourcemaps
    .pipe(buffer())
    .pipe(sourcemaps.init({ loadMaps: true })) // loads map from browserify file
    .pipe(sourcemaps.write('./')) // writes .map file
    //
    .pipe(gulp.dest('./dist/js'))
    .pipe(notify({
      message: "Build OK: <%= file.relative %>"
    }));
}

gulp.task('js', bundle); // so you can run `gulp js` to build the file 
