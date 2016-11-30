import React from 'react';
import Form from 'muicss/lib/react/form';
import Input from 'muicss/lib/react/input';
import Button from 'muicss/lib/react/button';
import SkyLight from 'react-skylight';

export default class ModalAddNode extends React.Component {
  constructor(props) {
      super(props);
  }

  handleSubmit(host, ip, port) {
    this.props.submit(host, ip, port);
    this.self.hide();
  };

  componentDidUpdate() {
    if (this.props.active && this.self)
      this.self.show();
  }

  render() {
    return (
      <SkyLight ref={el => this.self=el||this.self} >
        <Form>
          <legend>Login</legend>
          <Input name="host" label="Host" floatingLabel={true} required={true} />
          <Input name="ip" label="IP" floatingLabel={true} required={true} />
          <Input name="port" label="Port" floatingLabel={true} required={true} />
          <Button variant="raised"
            onClick={ev => {
              ev.preventDefault();
              var form = ev.target.parentNode;
              this.handleSubmit(form.host.value, form.ip.value, form.port.value);
            }}>
            Submit
          </Button>
        </Form>
      </SkyLight>
    );
  }
}
