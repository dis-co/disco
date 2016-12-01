import React from 'react';
import SkyLight from 'react-skylight';
import Login from './modals/Login';
import AddNode from './modals/AddNode';
import { MODALS } from './Constants';

export default class ModalAddNode extends React.Component {
  constructor(props) {
      super(props);
  }

  componentDidUpdate() {
      if (this.state && this.state.content) {
          this.self.show();
      }
  }

  handleSubmit() {
      this.setState({content:null})
      this.self.hide();
      if (this.state && typeof this.state.onSubmit == "function") {
          this.state.onSubmit();
      }
  }

  renderInner() {
    if (this.state == null)
        return null;

    const onSubmit = this.handleSubmit.bind(this);
    switch (this.state.content) {
        case MODALS.LOGIN:
            return <Login info={this.props.info} onSubmit={onSubmit} />
        case MODALS.ADD_NODE:
            return <AddNode info={this.props.info} onSubmit={onSubmit} />
      }
  }

  render() {
    return (
      <SkyLight ref={el => this.self=(el||this.self)} >
        {this.renderInner()}
      </SkyLight>
    );
  }
}
