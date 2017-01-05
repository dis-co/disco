import React from 'react';
import SkyLight from 'react-skylight';
import { loadModal } from "./Util.ts";

export default class ModalDialog extends React.Component {
  constructor(props) {
      super(props);
  }

  componentDidUpdate() {
      if (this.state && this.state.content) {
        this.self.show();
      }
  }

  handleSubmit() {
    //   this.setState({content:null})
      this.self.hide();
      if (this.state && typeof this.state.onSubmit == "function") {
          this.state.onSubmit();
      }
  }

  renderInner() {
    if (this.state == null || this.state.content == null) {
        return null;
    }
    else {
        const onSubmit = this.handleSubmit.bind(this);
        return React.createElement(this.state.content, { info: this.props.info, onSubmit: onSubmit });
    }
  }

  render() {
    return (
      <SkyLight dialogStyles={{height: "inherit"}} ref={el => this.self=(el||this.self)} >
        {this.renderInner()}
      </SkyLight>
    );
  }
}
