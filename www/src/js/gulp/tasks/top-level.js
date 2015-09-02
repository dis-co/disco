'use strict';
var gulp       = require('gulp');

/**
 * __Build everything__
 */
gulp.task('all', ['docs', 'assets', 'js', 'html']);


/**
 * __Default task__
 */
gulp.task('default', ['assets', 'js', 'html']);
