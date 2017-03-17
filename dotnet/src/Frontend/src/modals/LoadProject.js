import React from 'react'
import Form from 'muicss/lib/react/form'
import Input from 'muicss/lib/react/input'
import Button from 'muicss/lib/react/button'
import { findParentTag } from '../Util.ts'

export default function (props) {
  return (
    <Form>
      <legend>Load Project</legend>
      <Input name="name" label="Project name" floatingLabel={true} required={true} />
      <Input name="username" label="Username" floatingLabel={true} required={true} />
      <Input name="password" label="Password" type="password" floatingLabel={true} required={true} />
      <Button variant="raised"
        onClick={ev => {
          ev.preventDefault();
          var form = findParentTag(ev.target, "form");
          Iris.loadProject(
            form.name.value,
            form.username.value,
            form.password.value
          );
        }}>
        Submit
      </Button>
    </Form>
  );
}
