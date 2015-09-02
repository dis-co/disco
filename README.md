# About

This is the umbrella git repository for all components that constitute the Iris
media server. Its meant as a central point to collect and keep track of
sub-projects (mostly in _git submodules_ linked to this repo) and documentation.

# Checkout

To check out everything, clone this repository and run:

```shell
$ git submodule update --init --recursive
```

This will checkout all associated git repos and their submodules, respectively.

# Structure

A quick overview over the separate components:

- `vvvv`:     native code and patches
- `www`:      browser-side code (js/html/css)
- `wiki`:     documentation for Iris

# Making a release:

```shell
make
```

That's it. :)
