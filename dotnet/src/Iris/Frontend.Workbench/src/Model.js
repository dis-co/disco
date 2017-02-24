export default class Model {
  constructor(dispatch) {
    this.dispatch = diff => dispatch(diff, newState => {
      this.state = newState
    });
    this.state = {
      widgets: [],
      logs: []
    };
  }

  addWidget(widget) {
    this.dispatch({
      widgets: this.state.widgets.concat(widget)
    })
  }
}