var set = function(key, value) {
  var wrapper = {
    value: value
  };

  localStorage.setItem(key, JSON.stringify(wrapper));
};
    
var Settings = {
  set:  set,
  save: set,
  get: function(key) {
    try {
      var obj = JSON.parse(localStorage.getItem(key));
      return obj.value;
    }
    catch(e) {
      return null;
    }
  }
};

module.exports = Settings;
