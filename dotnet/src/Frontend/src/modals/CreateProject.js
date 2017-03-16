import React from 'react'
import Form from 'muicss/lib/react/form'
import Input from 'muicss/lib/react/input'
import Button from 'muicss/lib/react/button'
import css from 'muicss/dist/css/mui-noglobals.min.css'

export default function(props) {
  return (
    <Form>
      <legend>Project</legend>
      <Input name="project" label="Name" floatingLabel={true} required={true} />
      <Input name="bind" label="IP Address" floatingLabel={true} required={true} />
      <Input name="api" label="Api Port" floatingLabel={true} required={true} />
      <Input name="raft" label="Raft Port" floatingLabel={true} required={true}  />
      <Input name="ws" label="Web Socket Port" floatingLabel={true} required={true} />
      <Input name="git" label="Git Daemon Port" floatingLabel={true} required={true}  />
      <Button variant="raised"
        onClick={ev => {
          ev.preventDefault();
          var form = ev.target.parentNode;
          Iris.createProject({
            name: form.project.value,
            bind: form.bind.value,
            api: form.api.value,
            raft: form.raft.value,
            ws: form.ws.value,
            git: form.git.value
          });
          props.onSubmit();
        }}>
        Submit
      </Button>
    </Form>
  );
}
