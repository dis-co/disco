import React from 'react'
import Form from 'muicss/lib/react/form'
import Button from 'muicss/lib/react/button'

export default function (props) {
  return (
    <Form>
      <legend>{props.title}</legend>
      <p>{props.text}</p>
      {props.buttons.map((btn, i) =>
        <Button variant="raised"
          onClick={ev => {
            ev.preventDefault();
            props.onSubmit(btn[1]);
          }}>
          {btn[0]}
        </Button>
      )}
    </Form>
  );
}
