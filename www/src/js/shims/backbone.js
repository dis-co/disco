/**
 * Patch Backbone for browserify. Since there are no (or hardly any) top-level
 * variables, we need to patch jquery into Backbone manually.
 */
require('backbone').$ = window.$;
