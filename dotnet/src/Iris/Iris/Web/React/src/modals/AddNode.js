import React from 'react';
import Form from 'muicss/lib/react/form';
import Input from 'muicss/lib/react/input';
import Button from 'muicss/lib/react/button';
import { addNode } from "iris";

export default function(props) {
  return (
    <Form>
      <legend>Node information</legend>
      <Input name="host" label="Host" floatingLabel={true} required={true} />
      <Input name="ip" label="IP" floatingLabel={true} required={true} />
      <Input name="port" label="Port" floatingLabel={true} required={true} />
      <Button variant="raised"
        onClick={ev => {
          ev.preventDefault();
          var form = ev.target.parentNode;
          addNode(props.info, form.host.value, form.ip.value, form.port.value);
          props.onSubmit();
        }}>
        Submit
      </Button>
    </Form>
  );
}
