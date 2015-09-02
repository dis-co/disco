# Abstract

The Browser front-end to the Iris media server.


# Install

All dependencies are bundled up using the node package manager
(`npm`). Furthermore, we use the `gulp` build-tool, as well as
`browserify` to enable use of CommonJS modules in the browser;

Install global gulp and browserify commands:

```shell
$ npm install -g gulp browserify docker bower
```

To install local dependencies, use `bower` and `npm` like so:


```shell
$ npm install
$ bower install
```

# Compiling

To build the bundled front-end application, run `gulp all`. This will create a
folder `dist/` in the local directory, containing HTML, CSS and JavaScript build
artefacts.


# Docs

The docs get generated along with everything else when running `gulp all`, but
can also be compiled separately with the `gulp docs` command. They will be
copied to `dist/docs`.
