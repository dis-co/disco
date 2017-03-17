import React from 'react';
import Form from 'muicss/lib/react/form';
import Input from 'muicss/lib/react/input';
import Button from 'muicss/lib/react/button';
import { addMember } from "iris";

export default function(props) {
  return (
    <Form>
      <legend>Node information</legend>
      <Input name="id" label="Id" floatingLabel={true} required={true} />
      <Input name="host" label="Host" floatingLabel={true} required={true} />
      <Input name="ip" label="IP" floatingLabel={true} required={true} />
      <Input name="port" label="Port" floatingLabel={true} required={true} />
      <Input name="wsPort" label="Web Socket Port" floatingLabel={true} required={true} />
      <Input name="gitPort" label="Git Port" floatingLabel={true} required={true} />
      <Input name="apiPort" label="API Port" floatingLabel={true} required={true} />
      <Button variant="raised"
        onClick={ev => {
          ev.preventDefault();
          var form = ev.target.parentNode;
          addMember(props.info, form.id.value, form.host.value, form.ip.value, form.port.value,
                      form.wsPort.value, form.gitPort.value, form.apiPort.value);
          props.onSubmit();
        }}>
        Submit
      </Button>
    </Form>
  );
}
