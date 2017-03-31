import React from 'react'
import Form from 'muicss/lib/react/form'
import Input from 'muicss/lib/react/input'
import Button from 'muicss/lib/react/button'
import Dropdown from 'muicss/lib/react/dropdown';
import DropdownItem from 'muicss/lib/react/dropdown-item';
import { findParentTag } from '../Util.ts'

function getHandler(props, tag) {
  return ev => {
    ev.preventDefault();
    var form = findParentTag(ev.target, "form");
    props.onSubmit(form[tag].value);    
  }
}

export default function (props) {
  return (
    <Form>
      <legend>Project Config</legend>
      <p>Select a cluster in the project configuration or type a name to create a new one.</p>
      <p>The current machine will be added to the selected cluster if not present.</p>
      <Dropdown color="primary" name="select" label="Sites">
        props.sites.map((site,i) =>
          <DropdownItem key={i}>{site}</DropdownItem>)
      </Dropdown>
      <Button variant="raised" onClick={getHandler(props, "select")}>Select</Button>
      <Input name="create" label="New site" floatingLabel={true} required={false} />
      <Button variant="raised" onClick={getHandler(props, "create")}>Create</Button>
    </Form>
  );
}
