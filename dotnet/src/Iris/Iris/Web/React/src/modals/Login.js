import React from 'react';
import Form from 'muicss/lib/react/form';
import Input from 'muicss/lib/react/input';
import Button from 'muicss/lib/react/button';
import { login } from 'iris';
import { STATUS } from '../Constants';

export default function (props) {
  return (
    <Form>
      <legend>Login</legend>
      <Input name="username" label="Username" floatingLabel={true} required={true} />
      <Input name="password" label="Password" type="password" floatingLabel={true} required={true} />
      <Button variant="raised"
        onClick={ev => {
          ev.preventDefault();
          var form = ev.target.parentNode;
          props.onSubmit({
            username: form.username.value,
            password: form.password.value
          });
        }}>
        Submit
      </Button>
    </Form>
  );
}
