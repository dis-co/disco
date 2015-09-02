/**
 * <pre>
 *  _______                  _____              
 * |_     _|.--.--.--.-----.|     \.-----.-----. ™
 *   |   |  |  |  |  |  _  ||  --  |  -__|  -__|
 *   |___|  |________|_____||_____/|_____|_____|
 * </pre>                               
 *
 */

var _ = require('underscore');

/**
 * ### Constructor 
 * 
 * @param {object} options - options to pass to TwoDee instance
 *
 * The contructor processes the `options` object, and validates some values
 * passed. 
 */
var TwoDee = function(options) {
  if(options.min > options.max)
    throw("Error: min must be lower than max!");

  this.el       = options.el;
  this.min      = options.min    || 0;
  this.max      = options.max    || 127;
  this.width    = options.width  || 200;
  this.height   = options.height || 200;
  this.color    = options.color  || '#eee';
  this.handle   = options.handle || { size: 12, color: '#888888' };
  this.onChange = options.onChange;
  this._initialize(options);
};

/**
 * ### get
 * 
 * _(public)_
 *
 * Get the current value of TwoDee instance.
 */
TwoDee.prototype.get = function() {
  return this._fromPos(this.pos);

};

/**
 * ### set
 * 
 * _(public)_
 *
 * @param {object} values - object containing `x` & `y` coordinates, e.g. { x: 12, y: 83 }
 *
 * Set the value of the TwoDee instance to passed values. Values that are
 * out-of-bound are normalized to `min`/`max` values specififed.
 */
TwoDee.prototype.set = function(values) {
  if(values.x > this.max)
    values.x = this.max;

  if(values.y < this.min)
    values.y = this.min;
  
  this.pos = this._toPos(values);

  this.$handle.css({
    top: (this.pos.y * -1) + this.height,
    left: this.pos.x
  });
};

/**
 * ### _initialize
 * 
 * _(private)_
 * 
 * Builds the DOM elements needed and adds those to the target element.
 */
TwoDee.prototype._initialize = function() {
  switch(typeof this.el) {
  case 'string':
    this.el = $(this.el);
    break;
  case 'object':
    break;
  default:
    throw "The target element must be either a selector or element";
  }

  var instance = this;

  this.id  = this._guid();
  this.pos = { x: 0, y: 0 };

  var $ppt    = $('<div></div>');
  var $box    = $('<div id="' + this.id +'"></div>');
  var $handle = $('<div class="handle"></div>');

  // Construct the handle used to the change values with the mouse.
  $handle.css({
    'cursor':           'move',
    'width':            this.handle.size + 'px',
    'height':           this.handle.size + 'px',
    'border-radius':    this.handle.size + 'px',
    'background-color': this.handle.color,
    'line-height':      0,
    'font-size':        0
  });

  // The actual box containing the `draggable` should be the requested size
  // __plus__ the width/height of the handle.
  $box.css({
    'position':    'absolute',
    'left':        -1 * (this.handle.size / 2),
    'top':         -1 * (this.handle.size / 2),
    'width':       this.width  + this.handle.size,
    'height':      this.height + this.handle.size,
    'line-height': 0,
    'font-size':   0
  });

  // A mask (passepartout) that only shows the requested rectangle, offset by
  // half the width/height of the handle.
  $ppt.css({
    'background-color': this.color,
    'width':            this.width,
    'height':           this.height,
    'position':        'relative',
    'overflow':        'hidden',
    'cursor':          'pointer',
    'line-height':     0,
    'font-size':       0
  });

  // Putting the elements together.
  $box.html($handle);
  $ppt.html($box);

  this.el.append($ppt);

  // Handle 'mousedown' events (i.o.w. the first part of a 'click'). Since there
  // weird things going on with the handle and its constraints box, we normalize
  // the values.
  $box.on('mousedown', function(event, ui) {
    var rect = this.getBoundingClientRect();
    var left = event.clientX - rect.left - (instance.handle.size / 2);
    var top  = event.clientY - rect.top  - (instance.handle.size / 2);

    // Peg the lower bound to 0, if lower is requested.
    if(left < 0)
      left = 0;

    // Peg & round the upper bound to `this.width`. If the requested value is
    // higher than `this.width - 1px`, we round the value.
    if(left < instance.width && left > (instance.width - 1))
      left = Math.round(left);

    // Enforce an upper bound.
    if(left > instance.width)
      left = instance.width;

    // Set position of the handle to the normalized values...
    $handle.css({ left: left, top: top });

    // ...and process the coordinates into values in our domain.
    instance._handleChange.call(instance, {
      x: left,
      y: Math.abs(top - instance.height)
    });
  });

  // Shared start/drag/stop handler for draggable.
  var handler = function(e, ui) {
    var pos = $(this).position();

    // Peg the lower bound to 0, preventing strange values.
    if(ui.position.left < 0)
      ui.position.left = 0;

    // Peg & round the upper bound to instance.width
    if(ui.position.left < instance.width && ui.position.left > (instance.width - 1))
      ui.position.left = Math.round(ui.position.left);

    // Peg the upper bound (instance width), preventing strange values.
    if(ui.position.left > instance.width)
      ui.position.left = instance.width;

    // and process the coordinates into values in our domain
    instance._handleChange.call(instance, {
      x: pos.left, y: Math.abs(pos.top - instance.height)
    });
  };

  // Add draggable to $handle, with $box as containment. 
  $handle.draggable({
    containment: $box,
    scroll: false,
    start: handler,
    drag:  handler,
    stop:  handler 
  });

  // Rember the $handle element for later
  this.$handle = $handle;
};

/**
 * ### _guid
 * 
 * _(private)_
 * 
 * Generate a unique `id` to be used in DOM elements.
 */
TwoDee.prototype._guid = (function() {
  function s4() {
    return Math.floor((1 + Math.random()) * 0x10000)
               .toString(16)
               .substring(1);
  }
  return function() {
    return s4() + s4() + '-' + s4() + '-' + s4() + '-' +
           s4() + '-' + s4() + s4() + s4();
  };
})();

/**
 * ### _handleChange
 * 
 * _(private)_
 *
 * Handles the changes on the `$handle`, calling the user-supplied callback with
 * the normalized position information.
 */
TwoDee.prototype._handleChange = function(position) {
  this.pos = position;

  if(typeof this.onChange === 'function') {
    this.onChange(this._fromPos(position));
  }
};

/**
 * ### _fromPos
 *
 * _(private)_
 *
 * Convert absolute coordinates in px to mapped values in the range specified
 * during construction.
 */
TwoDee.prototype._fromPos = function(pos) {
  var minabs = Math.abs(this.min);
  var xfac = 0.01 * (pos.x / (this.width * 0.01));
  var yfac = 0.01 * (pos.y / (this.height * 0.01));
  var scale = (this.max + minabs);
  return {
    x: (xfac * scale) - minabs,
    y: (yfac * scale) - minabs
  };
};

/**
 * ### _toPos
 *
 * Convert values in the domain specified to absolute coordinates on the 2d
 * grid.}
 */
TwoDee.prototype._toPos = function(pos) {
  var minabs = Math.abs(this.min);
  var scale = 0.01 * (minabs + this.max);
  var xfac = pos.x / scale;
  var yfac = pos.y / scale;
  var hfac = (this.height * 0.01);
  var wfac = (this.width  * 0.01);
  return {
    x: (xfac * wfac) + ((minabs / scale) * wfac),
    y: (yfac * hfac) + ((minabs / scale) * hfac)
  };
};


module.exports = TwoDee;
