# Choosing a UI framework for Iris

## [Web components](https://developer.mozilla.org/en-US/docs/Web/Web_Components)

Reusable user interface widgets that are created using open Web technology.

**Pros**

- Browser native
- CSS encapsulation
- Custom HTML tags

**Cons**

- [Not supported yet by all browsers](http://jonrimmer.github.io/are-we-componentized-yet/),
  though it can be made compatible with [Polymer](https://www.polymer-project.org/1.0/)
- The [ecosystem](https://customelements.io/) is not as big and seems
  to be very centered in [Google Material UI](https://material.google.com/) guidelines

## [React](https://facebook.github.io/react/)

A JS library to build UIs using composable components, by Facebook

**Pros**

- Huge [ecosystem](https://react.rocks/), many components (grid layout, timeline)
  are already available
- Uses Virtual-DOM to increase performance
- Can easily be combined with other tools to control
  the state of the application, like [Redux](https://github.com/gaearon/redux-devtools#redux-devtools)
- Possibility of server side rendering

**Cons**

- No browser native
- To encapsulate CSS, it must be turned into [JS objects](https://facebook.github.io/react/docs/dom-elements.html#style)

## [Open MCT](https://nasa.github.io/openmct/)

Next-generation mission control framework being developed at NASA

**Pros**

- Already made solution very close to what we need
- Very professional and polished look
- Custom widgets can be created with plugins
- [Extensive documentation](https://nasa.github.io/openmct/docs/guide/)

**Cons**

- The whole framework needs to be adapted in order to use it
- There's a learning curve, and we may find it doesn't totally fit
  our needs after investing some time in the technology
- Uses [AngularJS 1](https://angularjs.org/), which is already being
  phased out in favour of AngularJS 2 (mutually incompatible)