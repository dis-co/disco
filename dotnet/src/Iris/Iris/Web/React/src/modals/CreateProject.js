import React from 'react';
import Form from 'muicss/lib/react/form';
import Input from 'muicss/lib/react/input';
import Button from 'muicss/lib/react/button';
import { createProject } from "iris";

export default function(props) {
  return (
    <Form>
      <legend>Node information</legend>
      <Input name="project" label="Project Name" floatingLabel={true} required={true} />
      <Input name="bind" label="IP Address" floatingLabel={true} required={true} />
      <Input name="git" label="Git Daemon Port" floatingLabel={true} required={true} />
      <Input name="ws" label="Web Socket Port" floatingLabel={true} required={true} />
      <Input name="raft" label="Raft Port" floatingLabel={true} required={true} />
      <Button variant="raised"
        onClick={ev => {
          ev.preventDefault();
          var form = ev.target.parentNode;
          createProject(props.info, form.project.value, form.bind.value, form.git.value, form.ws.value, form.raft.value);
          props.onSubmit();
        }}>
        Submit
      </Button>
    </Form>
  );
}
