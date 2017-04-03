import React, { Component } from 'react'
import Form from 'muicss/lib/react/form'
import Input from 'muicss/lib/react/input'
import Button from 'muicss/lib/react/button'
import Dropdown from 'muicss/lib/react/dropdown';
import DropdownItem from 'muicss/lib/react/dropdown-item';
import { findParentTag } from '../Util.ts'

export default class ProjectConfig extends Component {
  constructor(props) {
    super(props);
    this.state = {};
  }

  render() {
    return (
      <Form>
        <legend>Project Config</legend>
        <p>
          Select a cluster in the project configuration or type a name to create a new one.
          The current machine will be added to the selected cluster if not present.
        </p>
        <div>
          <Dropdown color="primary" name="select" label="Sites">
            {this.props.sites.map((site,i) =>
              <DropdownItem key={i} onClick={ev => this.setState({selected: site})}>{site}</DropdownItem>)}
          </Dropdown>
          <Input style={{
            display: "inline-block",
            marginLeft: 20
          }} label="Site" floatingLabel={true} required={false} value={this.state.selected} onChange={ev => this.setState({selected: ev.target.value})} />
        </div>
        <Button variant="raised" enabled={!!this.state.selected} onClick={ev => {
            ev.preventDefault();
            this.props.onSubmit(this.state.selected);
          }}>
          Select
        </Button>
      </Form>
    );
  }
}
