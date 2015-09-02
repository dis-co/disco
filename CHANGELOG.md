# Current Version: 0.4.0

### Changes sinc: 0.3.2

* add a DebugString node for sending arbitrary values to the browser
* support creating dynamic, filtered patch views
* optimize CallCue performance

### Changes since 0.3.1

* fix nasty bug that prevented `0` from being used as input value to number box.

### Changes since 0.3.0

* Reverting a Cluster configuration now completes even if a cluster member and
  its services are not reachable. Closes #133.
* Projects are now sorted in alphabetical order. Closes #107.
* Validate a number pin spinner values _before_ updating the model, and indicate
  erroneous values visually. Closes #112.
* Make fs dialog wider to accomodate for metadata on the right.


