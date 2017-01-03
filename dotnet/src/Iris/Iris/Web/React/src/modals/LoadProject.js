import React from 'react';
import Form from 'muicss/lib/react/form';
import Input from 'muicss/lib/react/input';
import Button from 'muicss/lib/react/button';
import { loadProject } from "iris";

let projectDir = null;

export default function(props) {
  return (
    <Form>
      <legend>Select project folder</legend>
      <Input name="dir" label="Project Directory" floatingLabel={true} required={true} />
      <Button variant="raised"
        onClick={ev => {
          ev.preventDefault();
          var form = ev.target.parentNode;
          debugger;
          loadProject(props.info, form.dir.value);
          props.onSubmit();
        }}>
        Load
      </Button>
    </Form>
  );
}
