let counter = 0;

export default class Model {
  constructor(dispatch) {
    this.subscribers = new Map();
    this.state = {
      widgets: new Map(),
      logs: []
    };
  }

  subscribe(key, subscriber) {
    if (!this.subscribers.has(key)) {
      this.subscribers.set(key, new Map());
    }
    let id = counter++;
    this.subscribers.get(key).set(id, subscriber);
    return {
      dispose() {
        this.subscribers.get(key).delete(id);
      }
    }
  }

  __notify(key, value = this.state[key]) {
    if (this.subscribers.has(key)) {
      this.subscribers.get(key).forEach(subscriber => subscriber(value));
    }
  }

  __setState(key, value) {
    this.state[key] = value;
    this.__notify(key, value);
  }

  addWidget(widget) {
    this.state.widgets.set(counter++, widget);
    this.__notify("widgets");
  }

  removeWidget(id) {
    this.state.widgets.delete(id);
    this.__notify("widgets");
  }

  addLog(log) {
    this.state.logs.push(log);
    this.__notify("logs", this.state.logs);
  }
}