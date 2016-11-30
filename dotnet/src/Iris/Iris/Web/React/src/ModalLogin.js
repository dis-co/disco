import React from 'react';
import Form from 'muicss/lib/react/form';
import Input from 'muicss/lib/react/input';
import Button from 'muicss/lib/react/button';
import SkyLight from 'react-skylight';
import { login, getCurrentSession } from 'iris';

const STATUS = {
  AUTHORIZED: "Authorized",
  WAITING: "Waiting",
  UNAUTHORIZED: "Unauthorized"
}

export default class ModalLogin extends React.Component {
  constructor(props) {
      super(props);
      this.state = { username: "", password: "" };
  }

  handleSubmit(username, password) {
    login(this.props.info, username, password);
    this.setState({status: STATUS.WAITING});
  };

  shouldComponentUpdate(nextProps, nextState) {
    var session = getCurrentSession(nextProps.info);
    return session && session.Status.StatusType.ToString() !== this.state.status;
  }

  componentWillReceiveProps(nextProps) {
    var session = getCurrentSession(nextProps.info);
    if (session) {
      let status = session.Status.StatusType.ToString();
      switch (status) {
        case STATUS.AUTHORIZED:
          this.setState({ status })
          this.self.hide();
          break;
        case STATUS.UNAUTHORIZED:
          if (this.state.status === STATUS.WAITING) {
            this.setState({ status })
          }
          break;
      }
    }
  }

  getMessage() {
    switch (this.state.status) {
      case STATUS.AUTHORIZED:
        return "Login succeeded!";
      case STATUS.WAITING:
        return "Waiting for response";
      case STATUS.UNAUTHORIZED:
        return "Input data is not correct";
    }
  }

  render() {
    return (
      <SkyLight
        closeButtonStyle={{display: "none"}}
        ref={el => {
          if (el != null) {
            this.self=el;
            if (this.state.status !== STATUS.AUTHORIZED)
              el.show();
          }
        }}
      >
        <Form>
          <legend>Login</legend>
          <Input name="username" label="Username" floatingLabel={true} required={true} />
          <Input name="password" label="Password" type="password" floatingLabel={true} required={true} />
          <Button variant="raised"
            disabled={this.state.status == STATUS.WAITING}
            onClick={ev => {
              ev.preventDefault();
              var form = ev.target.parentNode;
              this.handleSubmit(form.username.value, form.password.value);
            }}>
            Submit
          </Button>
          <p>{this.getMessage()}</p>
        </Form>
      </SkyLight>
    );
  }
}
